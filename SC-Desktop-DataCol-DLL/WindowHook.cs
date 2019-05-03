using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC_Desktop_DataCol_DLL
{
    /// <summary>
    /// Windows API window event hooks.
    /// </summary>
    class WindowHook : IDisposable
    {
        EventManager em;

        public WindowHook(EventManager _em)
        {
            this.em = _em;
        }
        ~WindowHook()
        {

        }

        public void Dispose()
        {
        }
    }
}
