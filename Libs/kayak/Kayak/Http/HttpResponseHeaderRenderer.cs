namespace Kayak.Http
{
    internal interface IHeaderRenderer
    {
        void Render(Net.ISocket consumer, HttpResponseHead head);
    }

    internal class HttpResponseHeaderRenderer : IHeaderRenderer
    {
        public void Render(Net.ISocket socket, HttpResponseHead head)
        {
            string status = head.Status;
            System.Collections.Generic.IDictionary<string, string> headers = head.Headers;

            // XXX don't reallocate every time
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendFormat("HTTP/1.1 {0}\r\n", status);

            if (headers != null)
            {
                foreach (System.Collections.Generic.KeyValuePair<string, string> pair in headers)
                    foreach (
                        string line in pair.Value.Split(new[] {"\r\n"}, System.StringSplitOptions.RemoveEmptyEntries))
                        sb.AppendFormat("{0}: {1}\r\n", pair.Key, line);
            }

            sb.Append("\r\n");

            socket.Write(new System.ArraySegment<byte>(System.Text.Encoding.ASCII.GetBytes(sb.ToString())), null);
        }
    }
}