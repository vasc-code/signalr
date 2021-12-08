using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SignalRPoc.Helper
{
    public static class GoogleLoginHelper
    {
        private static T post<T>(string route, string body)
        {
            var request = (HttpWebRequest)WebRequest.Create(route);
            request.ContentType = "application/x-www-form-urlencoded";
            if (string.IsNullOrWhiteSpace(body))
            {
                request.Method = "GET";
            }
            else
            {
                var data = System.Text.Encoding.ASCII.GetBytes(body);
                request.Method = "POST";
                request.ContentLength = data.Length;
                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            var resp = responseString.FromJson<T>();
            return resp;
        }

        public async static Task<string> AsBase64Url(this string url)
        {
            using (var client = new HttpClient())
            {
                var bytes = await client.GetByteArrayAsync(url);
                return "data:image/jpeg;base64," + Convert.ToBase64String(bytes);
            }
        }

        public class GoogleTokenResponse
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string scope { get; set; }
            public string token_type { get; set; }
            public string id_token { get; set; }
        }

        private class GoogleTokenInfoResponse
        {
            public string iss { get; set; }
            public string azp { get; set; }
            public string aud { get; set; }
            public string sub { get; set; }
            public string hd { get; set; }
            public string email { get; set; }
            public string email_verified { get; set; }
            public string at_hash { get; set; }
            public string name { get; set; }
            public string picture { get; set; }
            public string given_name { get; set; }
            public string family_name { get; set; }
            public string locale { get; set; }
            public string iat { get; set; }
            public string exp { get; set; }
            public string alg { get; set; }
            public string kid { get; set; }
            public string typ { get; set; }
        }
        private class StateParameterFromGoogleRequest
        {
            public string fcmToken { get; set; }
            public string appName { get; set; }
            public string redirectUrl { get; set; }
        }
        public class GoogleUserInformation
        {
            public string Email { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string ImageUrl { get; set; }
            public string Image64 { get; set; }
            //public bool PrivacyPolicy { get; set; }
            //public string LinkPolicyPrivacy { get; set; }
            //public bool TermOfUse { get; set; }
            //public string LinkTermOfUse { get; set; }
        }
        private static readonly string clientId = "807009363345-pqhu3gmfj295du05492cg4aiabm3st80.apps.googleusercontent.com";
        private static readonly string clientSecret = "Ywe_ggC2WXkzf0vELjFfhF6I";
        private static string FullyQualifiedApplicationPath(HttpRequest req)
        {
            var appPath = string.Empty;
            var context = req.HttpContext;
            if (context != null)
            {
                var isLocalHost = context.Connection.LocalPort != 80;
                appPath = string.Format("{0}://{1}{2}",
                                        context.Request.Scheme,
                                        context.Request.Host.Host,
                                        context.Connection.LocalPort == 80 ? string.Empty : ":" + context.Connection.LocalPort
                                        );
            }
            if (!appPath.EndsWith("/"))
            {
                appPath += "/";
            }
            return appPath;
        }

        public static string GetGoogleLoginRoute(string redirectActionName, HttpRequest req)
        {
            if (Debugger.IsAttached)
            {
                ////rotas à serem cadastradas no console.developers.google.com
                //var host = FullyQualifiedApplicationPath(req);
                //if (host.EndsWith("/"))
                //{
                //    host = host.Substring(0, host.Length - 1);
                //}
                //var OrigensJavaScriptAutorizadas = host;
                //var URIsDeRedirecionamentoAutorizados = $"{FullyQualifiedApplicationPath(req)}{redirectActionName}";
            }
            var redirect = Uri.EscapeDataString(string.Concat(FullyQualifiedApplicationPath(req), redirectActionName));
            var googleOauthRoute = "https://accounts.google.com/o/oauth2/auth?scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email%20https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.profile&access_type=offline&include_granted_scopes=true&state=[[params]]&redirect_uri=[[redirect]]&response_type=code&client_id=[[googlewebclientid]]";
            return googleOauthRoute.Replace("[[params]]", string.Empty).Replace("[[redirect]]", redirect).Replace("[[googlewebclientid]]", clientId);
        }

        public static string ImgFromUrl(this string imgUrl)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    try
                    {
                        using (Stream stream = client.GetStreamAsync(imgUrl).ConfigureAwait(false).GetAwaiter().GetResult())
                        {
                            if (stream == null)
                                return (string)null;
                            byte[] buffer = new byte[16384];
                            using (MemoryStream ms = new MemoryStream())
                            {
                                while (true)
                                {
                                    int num = stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false).GetAwaiter().GetResult();
                                    int read;
                                    if ((read = num) > 0)
                                        ms.Write(buffer, 0, read);
                                    else
                                        break;
                                }
                                var img = Convert.ToBase64String(ms.ToArray());
                                return img;
                            }
                            buffer = (byte[])null;
                        }
                    }
                    catch (Exception ex)
                    {
                        return "";
                    }
                }
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static GoogleUserInformation GetGoogleLoginUserInfo(string redirectActionName, HttpRequest req)
        {
            var parametters = req.Query.Keys.Select(a => new KeyValuePair<string, string>(a, req.Query[a])).ToList();
            if (parametters.Any(a => a.Key.Equals("error", StringComparison.OrdinalIgnoreCase)) ||
                !parametters.Any(a => a.Key.Equals("code", StringComparison.OrdinalIgnoreCase)))
            {
                return null;
            }
            var redirect = Uri.EscapeDataString(string.Concat(FullyQualifiedApplicationPath(req), redirectActionName));
            var code = parametters.First(a => a.Key == "code").Value;
            var googleTokenRoute = "https://www.googleapis.com/oauth2/v4/token";
            var googleTokenInfo = "https://www.googleapis.com/oauth2/v3/tokeninfo?id_token=";
            //Faz as chamadas às APIs do google
            var postData = $@"code={code}&client_id={clientId}&client_secret={clientSecret}&redirect_uri={redirect}&grant_type=authorization_code";
            var resp = post<GoogleTokenResponse>(googleTokenRoute, postData);
            var userInfo = post<GoogleTokenInfoResponse>(googleTokenInfo + resp.id_token, null);
            var result = new GoogleUserInformation();
            result.ImageUrl = userInfo.picture;
            //result.Image64 = userInfo.picture.ImgFromUrl();
            result.Image64 = userInfo.picture.AsBase64Url().ConfigureAwait(false).GetAwaiter().GetResult();
            result.FirstName = userInfo.given_name;
            result.LastName = userInfo.family_name;
            result.Email = userInfo.email;
            req.HttpContext.SetUser(result);
            req.HttpContext.Response.Cookies.Append("user", result.ToJson(), new CookieOptions
            {
                IsEssential = true,
                Path = "/",
                Expires = new DateTimeOffset(DateTime.Now.AddHours(12))
            });
            return result;
        }

        public static void SetUser(this HttpContext context, GoogleUserInformation user)
        {
            context.Session.SetString("userInfo", user.ToJson(true));
            context.Session.CommitAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public static GoogleUserInformation GetUser(this HttpContext context)
        {
            var json = context.Session.GetString("userInfo");
            GoogleUserInformation user = null;
            if (string.IsNullOrWhiteSpace(json) == false)
            {
                user = json.FromJson<GoogleUserInformation>();
            }
            return user;
        }
    }
}
