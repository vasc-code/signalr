using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRPoc.Hubs
{
    public class ChatHub : Hub
    {
        public class User
        {
            public string Email { get; set; }
            public List<string> Sessions { get; set; }
            public string Image { get; set; }
        }
        public class MessageBody
        {
            public string EmailSender { get; set; }
            public string Message { get; set; }
            public string Date { get; set; }
        }
        public static class Storage
        {
            public static ConcurrentDictionary<string, User> Users = new ConcurrentDictionary<string, User>();
            public static List<MessageBody> Messages { get; set; } = new List<MessageBody>();

            public static void Add(string user, string id, string image)
            {
                lock (Users)
                {
                    Users.TryAdd(user, new User()
                    {
                        Email = user,
                        Image = image,
                        Sessions = new List<string>() { id }
                    });
                }
            }

            public static User GetUserByEmail(string email)
            {
                lock (Users)
                {
                    User userObj = null;
                    Users.TryGetValue(email, out userObj);
                    return userObj;
                }
            }

            public static User GetUserById(string id)
            {
                lock (Users)
                {
                    var userObj = Users.FirstOrDefault(a => a.Value.Sessions.Contains(id));
                    return userObj.Value ?? null;
                }
            }

            public static User GetUserByEmailOrId(string email, string id)
            {
                lock (Users)
                {
                    var userObj = Users.FirstOrDefault(a => a.Value.Email == email || a.Value.Sessions.Contains(id));
                    return userObj.Value;
                }
            }

            public static IEnumerable<User> GetUserFriends(string email)
            {
                lock (Users)
                {
                    var userObj = Users.Where(a => a.Value.Email != email && !string.IsNullOrWhiteSpace(a.Value.Email));
                    return userObj.Select(a => a.Value);
                }
            }

            public static void AddNewMessage(string user, string message, string date, string id)
            {
                var userObj = GetUserByEmailOrId(user, id);
                if (userObj == null)
                {
                    Add(user, id, "");
                    userObj = GetUserByEmailOrId(user, id);
                }
                if (userObj != null)
                {
                    lock (Messages)
                    {
                        Messages.Add(new MessageBody()
                        {
                            Date = date,
                            Message = message,
                            EmailSender = user
                        });
                    }
                }
            }
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(System.Exception exception)
        {
            var id = Context.ConnectionId;
            var userObj = Storage.GetUserById(id);
            if (userObj != null)
            {
                userObj.Sessions.Remove(id);
                Clients.AllExcept(id).SendAsync("UserHasExited", userObj.Email).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message, string date)
        {
            var id = Context.ConnectionId;
            Storage.AddNewMessage(user, message, date, id);

            await Clients.AllExcept(id).SendAsync("ReceiveMessage", user, message, date);
        }

        public async Task Login(string user, string image)
        {
            var id = Context.ConnectionId;

            var userObj = Storage.GetUserByEmailOrId(user, id);
            if (userObj == null)
            {
                Storage.Add(user, id, image);
                userObj = Storage.GetUserByEmailOrId(user, id);
            }
            else
            {
                userObj.Email = user;
                userObj.Image = image;
            }

            foreach (var friend in Storage.GetUserFriends(user))
            {
                await Clients.Client(id).SendAsync("NewUserArrived", friend.Email, friend.Image);
            }
            await Clients.Client(id).SendAsync("ChatHistory", Storage.Messages);
            await Clients.AllExcept(id).SendAsync("NewUserArrived", user);
        }
    }
}
