using ActorBackend.Config;
using ActorBackend.Data;
using ActorBackend.Utils;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    public class UserManagerGrain : UserManagerGrainBase
    {
        private AppConfig config;
        private ILogger logger;

        private IMqttClient mqttClient;

        public UserManagerGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
            logger = Proto.Log.CreateLogger<UserManagerGrain>();

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());
            SubsbribeToNewConnections();
        }

        private void SubsbribeToNewConnections()
        {
            mqttClient.ApplicationMessageReceivedAsync += async args =>
            {
                var message = args.ApplicationMessage;
                var messageTokens = args.ApplicationMessage.ConvertPayloadToString().Split("<>");

                Neo4jQuery neo4jQuery = new Neo4jQuery();
                if (message.Topic == MqttTopicHelper.UserManagerUsers() && messageTokens[0] == "create")
                {
                    var query = JsonConvert.DeserializeObject<CreateUserQuery>(messageTokens[1])!;
                    neo4jQuery.CreateUserInfo = new CreateUserQueryInfo {
                        Info = new RequestInfo
                        {
                            RequestId = query.RequestId,
                            ClientActorIdentity = query.ClientId,
                            Operator = new User
                            {
                                Username = query.Account,
                                Password = query.AccountPassword
                            }
                        },
                        Creating = new User
                        {
                            Username = query.Username,
                            Password = query.Password
                        }
                    };
                }
                else if (message.Topic == MqttTopicHelper.UserManagerUsers() && messageTokens[0] == "delete")
                {
                    var query = JsonConvert.DeserializeObject<DeleteUserQuery>(messageTokens[1])!;
                    neo4jQuery.DeleteUserInfo = new DeleteUserQueryInfo
                    {
                        Info = new RequestInfo
                        {
                            RequestId = query.RequestId,
                            ClientActorIdentity = query.ClientId,
                            Operator = new User
                            {
                                Username = query.Account,
                                Password = query.AccountPassword
                            }
                        },
                        Username = query.Username
                    };
                }
                else if (message.Topic == MqttTopicHelper.UserManagerRoles() && messageTokens[0] == "add")
                {
                    var query = JsonConvert.DeserializeObject<RoleQuery>(messageTokens[1])!;
                    neo4jQuery.AddRoleInfo = new AddRoleQueryInfo
                    {
                        Info = new RequestInfo
                        {
                            RequestId = query.RequestId,
                            ClientActorIdentity = query.ClientId,
                            Operator = new User
                            {
                                Username = query.Account,
                                Password = query.AccountPassword
                            }
                        },
                        Username = query.Username,
                        Role = query.Role
                    };
                }
                else if (message.Topic == MqttTopicHelper.UserManagerRoles() && messageTokens[0] == "remove")
                {
                    var query = JsonConvert.DeserializeObject<RoleQuery>(messageTokens[1])!;
                    neo4jQuery.RemoveRoleInfo = new RemoveRoleQueryInfo
                    {
                        Info = new RequestInfo
                        {
                            RequestId = query.RequestId,
                            ClientActorIdentity = query.ClientId,
                            Operator = new User
                            {
                                Username = query.Account,
                                Password = query.AccountPassword
                            }
                        },
                        Username = query.Username,
                        Role = query.Role
                    };
                }

                var resolver = Context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER);
                await resolver.ResolveQuery(neo4jQuery, CancellationToken.None);

            };
            mqttClient.SubscribeAsync(MqttTopicHelper.UserManagerUsers());
            mqttClient.SubscribeAsync(MqttTopicHelper.UserManagerRoles());

            logger.LogInformation($"User Manager Started");
        }
    }
}
