using System;
using System.Collections.Generic;
using System.Text;

namespace SPIXI
{
    public class EventArgs<T> : EventArgs
    {
        public T Value { get; private set; }

        public EventArgs(T val)
        {
            Value = val;
        }
    }
}
