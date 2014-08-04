using Microsoft.SPOT;

namespace XSockets.ClientMF42.Event
{
    public delegate void EventHandler(object sender, EventArgs e);

    public class MyEventHandler
    {
        private event EventHandler h;
        public MyEventHandler(EventHandler handler)
        {
            h = handler;
        }

        public void Invoke(object sender, EventArgs e)
        {
            this.h(sender, e);
        }
    }
}
