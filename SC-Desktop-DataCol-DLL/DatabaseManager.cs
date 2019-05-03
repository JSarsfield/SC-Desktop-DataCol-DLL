using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{
    /// <summary>
    /// Handles all database transactions. Manages other database classes.
    /// This class only communicates with MainWindow class (excluding other database classes).
    /// DatabaseManager follows TAP design pattern https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap?view=netframework-4.7.2
    /// Each username will be a collection.
    /// Collections: Users, Sessions, Rooms, Days (Use references and $lookup commands) https://docs.mongodb.com/manual/reference/database-references/
    /// OR only Sessions collection with compound index??
    /// db.Collection.createIndex speedup lookup with this https://docs.mongodb.com/manual/tutorial/optimize-query-performance-with-indexes-and-projections/
    /// Compound index speeds up queries, ORDER OF FIELDS MATTERS. Sessions collection compound index: Day->Room->Username->timestamp. Session document has ref to User document in Users collection (stores session refs).
    /// Tutorial c# mongdb https://rubikscode.net/2017/10/02/using-mongodb-in-c/
    /// Documentation (Important columns on left e.g. Data Models) https://docs.mongodb.com/manual/
    /// ALL network IO tasks should be async Task
    /// 
    /// Store RecordStartTime, RecordEndTime (update with each write or periodically to register inactvity), LogonTime?, WindowsSessionID? (May not need this)
    /// </summary>
    class DatabaseManager
    {
        private readonly string connection_url = "mongodb+srv://abc:abc@sc-test-3l5d7.mongodb.net/test?retryWrites=true";

        public IMongoClient Client { get; set; }  // Client connection (user)
        public IMongoDatabase Db { get; set; }  // Database
        public IMongoCollection<User> Users { get; set; }  // Database
        public IMongoCollection<Session> Sessions { get; set; }  // Database
        public string Username { get; set; }
        public DateTime Logon { get; set; }
        private Session session;  // Store session data
        private EventManager emRef;  // Reference to event manager object
        private int flushTime = 5000;  // Milliseconds to wait before flushing activity data to DB
        private Stopwatch sw = new Stopwatch();
        private bool isSessionRunning = false;
        public string sessionEndReason = "";


        public DatabaseManager(string username, DateTime login_time)
        {
            this.Username = username;
            this.Client = new MongoClient(connection_url);  // No network IO, async unnecssary
            this.Db = this.Client.GetDatabase("SC");  // No network IO, async unnecssary
            this.Users = this.Db.GetCollection<User>("Users");   // No network IO, async unnecssary
            this.Sessions = this.Db.GetCollection<Session>("Sessions");   // No network IO, async unnecssary
        }


        ~DatabaseManager()
        {

        }


        public async Task CreateSession(EventManager _emRef)  // Create session object.
        {
            emRef = _emRef;
            session = new Session(this.Username);
            await Sessions.InsertOneAsync(this.session);
            Console.WriteLine("Session ID: " + this.session.Id.ToString());
            isSessionRunning = true;
        }


        /// <summary>
        /// Send activity data to session until Task is cancelled
        /// </summary>
        /*
        public Task SendActivityData(CancellationToken ct)
        {
            while (true)
            {
                if (!sw.IsRunning)
                    sw.Start();

                Thread.Sleep(100); // 100 ms wait, then check cancellation token, this needs to be quick as system may be shutting down

                // Thread sleep here remaining milliseconds if greater than 0
                if (ct.IsCancellationRequested || sw.ElapsedMilliseconds > flushTime)
                {
                    if (emRef.activityQueue.Count > 0)
                    {
                        var activityQueueClone = new List<Dictionary<string, object>>();
                        lock (emRef.activityQueue)
                        {
                            foreach (var dict in emRef.activityQueue)
                            {
                                activityQueueClone.Add(new Dictionary<string, object>(dict));
                            }
                            emRef.activityQueue.Clear();
                        }
                        FilterDefinition<Session> filter = Builders<Session>.Filter.Eq(x => x.Id, this.session.Id);
                        UpdateDefinition<Session> update = Builders<Session>.Update.PushEach<Dictionary<string, object>>("activity", activityQueueClone);
                        Sessions.UpdateOneAsync(filter, update);

                    }
                    // Attempt to resend failed activity data
                    // If sending activity data fails, store it in temp queue for resending.
                    // If temp queue gets too big or no network for X seconds then end session and close
                    // Update lastactivity timestamp in DB
                    if (ct.IsCancellationRequested) // Check cancellation token. Token cancelled in MainWindow.cs -> StopRecording.
                    {
                        FilterDefinition<Session> filter = Builders<Session>.Filter.Eq(x => x.Id, this.session.Id);
                        UpdateDefinition<Session> update = Builders<Session>.Update.Set("ended", sessionEndReason)
                            .Set("endTime", DateTime.UtcNow);
                        Sessions.UpdateOneAsync(filter, update);
                        emRef = null;  // This will be set to a new instance of eventmanger is recording restarts.
                        return null; // End Task
                    }
                    sw.Reset();
                }
            }
        }*/


        public async Task GetOrCreateUser()  // TODO handle exceptions from async Tasks
        {
            var builder = Builders<User>.Filter;
            var filter = builder.Eq(x => x.Username, this.Username);
            var update = Builders<User>.Update
                .SetOnInsert(x => x.Username, this.Username);
            var options = new UpdateOptions { IsUpsert = true };

            await Users.UpdateOneAsync(filter, update, options);
        }


        public async Task<Nullable<bool>> CheckIfOptIn()
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Eq("username", this.Username);
            var res = await Users.Find(filter).Limit(1).Project(Builders<User>.Projection.Include(x => x.Optin).Exclude(x => x.Id)).SingleAsync();
            return !res.Contains("optin") ? null : res.GetValue(0).AsNullableBoolean;
        }


        public async Task SetOptIn(bool IsOptIn)
        {
            FilterDefinition<User> filter = Builders<User>.Filter.Eq("username", this.Username);
            UpdateDefinition<User> update = Builders<User>.Update.Set("optin", IsOptIn);
            await Users.UpdateOneAsync(filter, update);
        }
    }
}
