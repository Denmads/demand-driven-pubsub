using ActorBackend.Data;
using ActorBackend.Utils;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;
using IResult = Neo4j.Driver.IResult;

namespace ActorBackend.Actors.Neo4jGrain
{

    public partial class Neo4jQueryGrain : Neo4jQueryGrainBase
    {
        private IResult? ExecuteCypher(string cypher, bool write = false)
        {
            if (write)
            {
                return neo4jSession.ExecuteWrite(tx =>
                {
                    var result = tx.Run(cypher);
                    return result;
                });
            }
            else
            {
                return neo4jSession.ExecuteRead(tx =>
                {
                    var result = tx.Run(cypher);
                    return result;
                });
            }
        }

        public override async Task ResolveQuery(Neo4jQuery request)
        {
            try
            {
                if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.PublishInfo)
                {
                    await ResolvePublishQuery(request.PublishInfo);
                }
                else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.SubscribeInfo)
                {
                    await ResolveSubscribeQuery(request.SubscribeInfo);
                }
                else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.CreateUserInfo)
                {
                    await ResolveSubscribeQuery(request.SubscribeInfo);
                }
                else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.DeleteUserInfo)
                {
                    await ResolveSubscribeQuery(request.SubscribeInfo);
                }
                else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.AddRoleInfo)
                {
                    await ResolveSubscribeQuery(request.SubscribeInfo);
                }
                else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.RemoveRoleInfo)
                {
                    await ResolveSubscribeQuery(request.SubscribeInfo);
                }
            }
            catch (Exception ex)
            {
                var info = GetInfo(request);

                var error = new ErrorResponse { Message = ex.Message };
                await Context.Cluster().GetClientGrain(info.ClientActorIdentity)
                    .QueryResult(new QueryResponse { RequestId = info.RequestId, ErrorResponse = error }, CancellationToken.None);
            }
            finally
            {
                await Context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER)
                    .QueryResolved(new QueryResolvedResponse { QueryActorIdentity = Context.ClusterIdentity()!.Identity }, CancellationToken.None);
            }
        }

        private RequestInfo GetInfo(Neo4jQuery query)
        {
            switch (query.QueryTypeCase)
            {
                case Neo4jQuery.QueryTypeOneofCase.PublishInfo: return query.PublishInfo.Info;
                case Neo4jQuery.QueryTypeOneofCase.SubscribeInfo: return query.SubscribeInfo.Info;
                case Neo4jQuery.QueryTypeOneofCase.CreateUserInfo: return query.CreateUserInfo.Info;
                case Neo4jQuery.QueryTypeOneofCase.DeleteUserInfo: return query.DeleteUserInfo.Info;
                case Neo4jQuery.QueryTypeOneofCase.AddRoleInfo: return query.AddRoleInfo.Info;
                case Neo4jQuery.QueryTypeOneofCase.RemoveRoleInfo: return query.RemoveRoleInfo.Info;
            }

            return null;
        }

        private bool HasAdminRole(User user)
        {
            return HasRoles(user, new string[] { "Admin" });
        }

        private bool HasRoles(User user, string[] requiredRoles)
        {
            var roles = GetUserRoles(user);
            return requiredRoles.All(x => roles.Contains(x));
        }

        private string[] GetUserRoles(User user)
        {
            string getUserCypher = $"MATCH (user:User {{username: '{user.Username}'}}) RETURN user";
            var res = ExecuteCypher(getUserCypher);

            if (res == null) //No user found
                return new string[0];

            var userRes = res.ElementAt(0)["user"] as INode;

            //Verify password
            var hashedPass = userRes!.Properties["password"].As<string>();

            if (!PasswordUtil.VerifyPassword(user.Password, hashedPass))
                return new string[0];

            //Get roles
            string getRolesCypher = $"MATCH (:User {{username: '{user.Username}'}})-[:HAS_ROLE]->(role:Role) RETURN role";
            var res2 = ExecuteCypher(getRolesCypher);

            if (res2 == null) //No roles found
                return new string[0];

            return (from role in res2 select ((INode)role["role"]).Properties["name"].As<string>()).ToArray();
        }

        private async Task SendSuccessResponseToClient(RequestInfo info)
        {
            var res = new QueryResponse { RequestId = info.RequestId, SuccessResponse = new SuccessResponse() };

            await Context.Cluster().GetClientGrain(info.ClientActorIdentity)
                    .QueryResult(res, CancellationToken.None);
        }

    }
}
