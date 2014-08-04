using XSockets.ClientMF42.Event.Arguments.Interfaces;

namespace XSockets.ClientMF42.Event
{
    public delegate void MessageHandler(object sender, IMessage e);

    public class MyMessageHandler
    {
        private event MessageHandler h;
        public MyMessageHandler(MessageHandler handler)
        {
            h = handler;
        }

        public void Invoke(object sender, IMessage e)
        {
            this.h(sender, e);
        }
    }
}
