﻿using System;
using System.IO;
using System.Net;
using System.Threading;
using AbaSoft.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Abacloud.ApiClient
{
    public class Client
    {
        private readonly string serverHost;
        private readonly Session session;
        private readonly string computerName;

        private Client(string a_serverHost, Session a_session)
        {
            computerName = Environment.MachineName;
            serverHost = a_serverHost;
            session = a_session;
        }

        public Session Session
        {
            get { return session; }
        }

        public static Client Create(string a_serverHost, string a_userName, string a_password)
        {
            Client _retVal = null;
            var _sesssion = createSession(a_serverHost, a_userName, a_password);
            if (_sesssion != null)
                _retVal = new Client(a_serverHost, _sesssion);
            return _retVal;
        }

        public static Client Create(string a_serverHost, Session a_session)
        {
            return new Client(a_serverHost, a_session);
        }

        private static Session createSession(string a_serverHost, string a_email, string a_password)
        {
            Session _retVal = null;
            var _request = WebRequest.Create(a_serverHost + "sessions/");
            _request.Method = "POST";

            TextWriter _tw = new StreamWriter(_request.GetRequestStream());
            JsonWriter _wr = new JsonTextWriter(_tw);
            _wr.WriteStartObject();
            _wr.WritePropertyName("email");
            _wr.WriteValue(a_email);
            _wr.WritePropertyName("password");
            _wr.WriteValue(a_password);
            _wr.WriteEndObject();
            //a_request.ContentLength = a_request.GetRequestStream().Length;
            _wr.Close();
            _tw.Close();
            var _response = (HttpWebResponse) _request.GetResponse();
            switch (_response.StatusCode)
            {
                case HttpStatusCode.Created:
// ReSharper disable AssignNullToNotNullAttribute
                    TextReader _tr = new StreamReader(_response.GetResponseStream());
// ReSharper restore AssignNullToNotNullAttribute
                    JsonReader _js = new JsonTextReader(_tr);
                    _js.Read();
                    _js.Read();
                    if (_js.Path == "sessionId")
                        _retVal = new Session(_js.ReadAsString());
                    _js.Close();
                    _tr.Close();
                    break;
                case HttpStatusCode.Forbidden:
                    break;
                default:
                    throw new NotSupportedException(string.Format("Не известный возврат состояния ответа {0}",
                        _response.StatusCode));
            }
            return _retVal;
        }

        public string SendContent(FileStream a_stream, out string a_errorMessage)
        {
            var _retVal = string.Empty;
            a_errorMessage = string.Empty;
            var _request = WebRequest.Create(serverHost + "//contents");
            _request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Session {0}", session));
            _request.Method = "PUT";
            copyStream(a_stream, _request);

            var _response = getResponse(_request);
            if (_response != null)
            {
                switch (_response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        var _jr = new JsonTextReader(new StreamReader(_response.GetResponseStream()));
                        _jr.Read();
                        _jr.Read();
                        if (_jr.Path != "guid")
                            throw new AbaCloudApiClientException("В ответе ожидалось поле guid");
                        _retVal = _jr.ReadAsString();
                        _jr.Close();
                        break;
                    case HttpStatusCode.Forbidden:
                        a_errorMessage = "Доступ запрещен";
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Не предусмотретный ответ сервера {0}",
                            _response.StatusCode));
                }
            }
            else
            {
                a_errorMessage = "Нет ответа от сервера";
            }
            return _retVal;
        }

        private static HttpWebResponse getResponse(WebRequest a_request)
        {
            HttpWebResponse _retval;

            // Метод GetResponse() вызовет исключение, если сервер вернет 4xx или 5xx
            try
            {
                _retval = (HttpWebResponse) a_request.GetResponse();
            }
            catch (WebException _exception)
            {
                _retval = (HttpWebResponse) _exception.Response;
            }

            return _retval;
        }

        private static void copyStream(Stream a_inStream, WebRequest a_request)
        {
            a_inStream.CopyTo(a_request.GetRequestStream());
            //const int bufSize = 1024*1024;
            //var _copyCount = 0;
            //var _length = a_inStream.Length;
            //int _ = 0;
            //while (_copyCount < _length)
            //{
            //    var _toCopyCount = (int) (((_length - _copyCount) > bufSize) ? bufSize : (_length - _copyCount));
            //    var _buf = new byte[_toCopyCount];
            //    a_inStream.Read(_buf, 0, _toCopyCount);
            //    a_request.GetRequestStream().Write(_buf, 0, _toCopyCount);
            //    _copyCount += _toCopyCount;
            //    _++;
            //}
        }

        private void copyStream(FileStream a_outStream, HttpWebResponse a_response)
        {
            a_response.GetResponseStream().CopyTo(a_outStream);
        }

        public bool SetTagForContent(string a_contentGuid, string a_tag, out string a_errorMessage)
        {
            var _retVal = false;
            a_errorMessage = string.Empty;

            var _request = WebRequest.Create(serverHost + string.Format("//contents//{0}//details//tags", a_contentGuid));
            _request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Session {0}", session));
            _request.Method = "PUT";
            JsonWriter _jw = new JsonTextWriter(new StreamWriter(_request.GetRequestStream()));
            _jw.WriteStartObject();
            _jw.WritePropertyName("tag");
            _jw.WriteValue(a_tag);
            _jw.WriteEndObject();
            _jw.Close();
            var _response = getResponse(_request);
            if (_response != null)
            {
                switch (_response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        _retVal = true;
                        break;
                    case HttpStatusCode.Forbidden:
                        a_errorMessage = "Доступ запрещен";
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Непредусмотретный ответ сервера {0}",
                            _response.StatusCode));
                }
            }
            else
            {
                a_errorMessage = "Нет ответа от сервера";
            }

            return _retVal;
        }

        public bool GetContent(string a_guid, FileStream a_stream, out string a_errorMessage)
        {
            var _retVal = false;
            a_errorMessage = string.Empty;

            var _request = WebRequest.Create(string.Format("{0}contents//{1}//", serverHost, a_guid));
            _request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Session {0}", session));
            _request.Method = "GET";

            var _response = getResponse(_request);
            if (_response != null)
            {
                switch (_response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        copyStream(a_stream, _response);
                        _retVal = true;
                        break;
                    case HttpStatusCode.Forbidden:
                        a_errorMessage = "Доступ запрещен";
                        break;
                    case HttpStatusCode.NotFound:
                        a_errorMessage = "Контент не найден";
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Непредусмотретный ответ сервера {0}",
                            _response.StatusCode));
                }
            }
            else
            {
                a_errorMessage = "Нет ответа от сервера";
            }

            return _retVal;
        }

        public bool ExistContent(string a_hash)
        {
            var _retVal = default(bool);

            var _request = WebRequest.Create(string.Format("{0}contents//?hash={1}//", serverHost, a_hash));
            _request.Headers.Add(HttpRequestHeader.Authorization, string.Format("Session {0}", session));
            _request.Method = "GET";

            var _response = getResponse(_request);
            if (_response != null)
            {
                switch (_response.StatusCode)
                {
                    case HttpStatusCode.OK:
                        _retVal = true;
                        break;
                    case HttpStatusCode.NotFound:
                        _retVal = false;
                        break;
                    default:
                        throw new NotSupportedException(string.Format("Непредусмотретный ответ сервера {0}",
                            _response.StatusCode));
                }
            }

            return _retVal;
        }

        public void SendFile(string a_fileName)
        {
            var _stream = File.OpenRead(a_fileName);
            string _error;
            var _guid = SendContent(_stream, out _error);
            _stream.Close();

            SetTagForContent(_guid, string.Format("SYSTEM$CUMPUTER_NAME {0}", computerName), out _error);
            SetTagForContent(_guid, string.Format("SYSTEM$FILE_NAME {0}", a_fileName), out _error);
        }
    }
}
