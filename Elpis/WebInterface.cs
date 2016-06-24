/*
 * Copyright 2015 - Alexey Seliverstov
 * email: alexey.seliverstov.dev@gmail.com
 *
 * This file is part of Elpis.
 * Elpis is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Elpis is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with Elpis. If not, see http://www.gnu.org/licenses/.
*/



namespace Elpis.Wpf
{
    internal class WebInterface
    {
        private Kayak.Net.IScheduler _scheduler;

        public void StartInterface()
        {
#if DEBUG
            System.Diagnostics.Debug.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(System.Console.Out));
            System.Diagnostics.Debug.AutoFlush = true;
#endif

            _scheduler = Kayak.Net.KayakScheduler.Factory.Create(new SchedulerDelegate());
            Kayak.Net.IServer server = Kayak.Http.HttpServerExtensions.CreateHttp(Kayak.Net.KayakServer.Factory, new RequestDelegate(),
                _scheduler);

            using (server.Listen(new System.Net.IPEndPoint(System.Net.IPAddress.Any, 35747)))
            {
                // runs scheduler on calling thread. this method will block until
                // someone calls Stop() on the scheduler.
                _scheduler.Start();
            }
        }

        public void StopInterface()
        {
            _scheduler.Stop();
        }

        private class SchedulerDelegate : Kayak.Net.ISchedulerDelegate
        {
            public void OnException(Kayak.Net.IScheduler scheduler, System.Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error on scheduler.");
                Kayak.Extensions.Extensions.DebugStackTrace(e);
            }

            public void OnStop(Kayak.Net.IScheduler scheduler) {}
        }

        private class RequestDelegate : Kayak.Http.IHttpRequestDelegate
        {
            public void OnRequest(Kayak.Http.HttpRequestHead request, Kayak.Net.IDataProducer requestBody,
                Kayak.Http.IHttpResponseDelegate response)
            {
                if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/next"))
                {
                    // when you subscribe to the request body before calling OnResponse,
                    // the server will automatically send 100-continue if the client is
                    // expecting it.
                    bool ret = Wpf.MainWindow.Next();

                    string body = ret ? "Successfully skipped." : "You have to wait for 20 seconds to skip again.";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/pause"))
                {
                    Wpf.MainWindow.Pause();
                    const string body = "Paused.";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/play"))
                {
                    Wpf.MainWindow.Play();
                    const string body = "Playing.";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/toggleplaypause"))
                {
                    string body = Wpf.MainWindow.Player.Playing ? "Paused." : "Playing.";
                    Wpf.MainWindow.PlayPauseToggle();

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/like"))
                {
                    Wpf.MainWindow.Like();
                    string body = "Like";
                    if (Wpf.MainWindow.GetCurrentSong().Loved)
                        body = "Liked";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/dislike"))
                {
                    Wpf.MainWindow.Dislike();
                    string body = "Disliked.";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/currentsong"))
                {
                    PandoraSharp.Song s = Wpf.MainWindow.GetCurrentSong();
                    string body = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(s);

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Method.ToUpperInvariant() == "GET" && request.Uri.StartsWith("/connect"))
                {
                    const string body = "true";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else if (request.Uri.StartsWith("/"))
                {
                    string body = $"Hello world.\r\nHello.\r\n\r\nUri: {request.Uri}\r\nPath: {request.Path}\r\nQuery:{request.QueryString}\r\nFragment: {request.Fragment}\r\n";

                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "200 OK",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", body.Length.ToString()}
                            }
                    };
                    response.OnResponse(headers, new BufferedProducer(body));
                }
                else
                {
                    string responseBody = "The resource you requested ('" + request.Uri + "') could not be found.";
                    Kayak.Http.HttpResponseHead headers = new Kayak.Http.HttpResponseHead
                    {
                        Status = "404 Not Found",
                        Headers =
                            new System.Collections.Generic.Dictionary<string, string>
                            {
                                {"Content-Type", "text/plain"},
                                {"Content-Length", responseBody.Length.ToString()}
                            }
                    };
                    BufferedProducer body = new BufferedProducer(responseBody);

                    response.OnResponse(headers, body);
                }
            }
        }

        private class BufferedProducer : Kayak.Net.IDataProducer
        {
            public BufferedProducer(string data) : this(data, System.Text.Encoding.UTF8) {}
            public BufferedProducer(string data, System.Text.Encoding encoding) : this(encoding.GetBytes(data)) {}
            public BufferedProducer(byte[] data) : this(new System.ArraySegment<byte>(data)) {}

            public BufferedProducer(System.ArraySegment<byte> data)
            {
                this.data = data;
            }

            private readonly System.ArraySegment<byte> data;

            public System.IDisposable Connect(Kayak.Net.IDataConsumer channel)
            {
                // null continuation, consumer must swallow the data immediately.
                channel.OnData(data, null);
                channel.OnEnd();
                return null;
            }
        }

        private class BufferedConsumer : Kayak.Net.IDataConsumer
        {
            public BufferedConsumer(System.Action<string> resultCallback, System.Action<System.Exception> errorCallback)
            {
                _resultCallback = resultCallback;
                _errorCallback = errorCallback;
            }

            private readonly System.Collections.Generic.List<System.ArraySegment<byte>> buffer =
                new System.Collections.Generic.List<System.ArraySegment<byte>>();

            private readonly System.Action<System.Exception> _errorCallback;
            private readonly System.Action<string> _resultCallback;

            public bool OnData(System.ArraySegment<byte> data, System.Action continuation)
            {
                // since we're just buffering, ignore the continuation.
                // TODO: place an upper limit on the size of the buffer.
                // don't want a client to take up all the RAM on our server!
                buffer.Add(data);
                return false;
            }

            public void OnError(System.Exception error)
            {
                _errorCallback(error);
            }

            public void OnEnd()
            {
                // turn the buffer into a string.
                //
                // (if this isn't what you want, you could skip
                // this step and make the result callback accept
                // List<ArraySegment<byte>> or whatever)
                //
                string str = "";
                if (buffer.Count > 0)
                {
                    str =
                        System.Linq.Enumerable.Aggregate(
                            System.Linq.Enumerable.Select(buffer,
                                b => System.Text.Encoding.UTF8.GetString(b.Array, b.Offset, b.Count)),
                            (result, next) => result + next);
                }

                _resultCallback(str);
            }
        }
    }
}