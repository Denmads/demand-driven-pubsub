using ActorBackend.Data;
using ActorBackend.Utils;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors.Neo4jGrain
{

    public partial class Neo4jQueryGrain : Neo4jQueryGrainBase
    {
        public async Task ResolveCreateUserQuery(CreateUserQueryInfo request)
        {
            if (!HasAdminRole(request.Info.Operator))
            {
                throw new Exception("Missing 'Admin' role to create user.");
            }

            //Ensure unique username
            string usernameUniqueQuery = $"MATCH (u:User {{username: '{request.Creating.Username}'}}) RETURN u";
            var res = ExecuteCypher(usernameUniqueQuery);
            if (res != null && res.Count() > 0)
            {
                throw new Exception("Username is already in use.");
            }

            string hashedPass = PasswordUtil.HashPassword(request.Creating.Password);
            string cypher = $"MERGE (:User {{username: '{request.Creating.Username}', password: '{hashedPass}'}})";

            ExecuteCypher(cypher, write: true);

            await SendSuccessResponseToClient(request.Info);
        }
        public async Task ResolveDeleteUserQuery(DeleteUserQueryInfo request)
        {
            if (!HasAdminRole(request.Info.Operator))
            {
                throw new Exception("Missing 'Admin' role to delete user.");
            }

            string cypher = $"MATCH (u:User {{username: '{request.Username}'}}) DELETE u";
            ExecuteCypher(cypher, write: true);

            await SendSuccessResponseToClient(request.Info);
        }

        public async Task ResolveAddRoleQuery(AddRoleQueryInfo request)
        {
            if (!HasAdminRole(request.Info.Operator))
            {
                throw new Exception("Missing 'Admin' role to add role to user.");
            }

            string cypher = $"MATCH (u:User {{username: '{request.Username}'}}) ";
            cypher += $"MATCH (r:Role {{name: '{request.Role}'}}) ";
            cypher += $"MERGE (u)-[:HAS_ROLE]->(r)";
            ExecuteCypher(cypher, write: true);

            await SendSuccessResponseToClient(request.Info);
        }

        public async Task ResolveRemoveRoleQuery(RemoveRoleQueryInfo request)
        {
            if (!HasAdminRole(request.Info.Operator))
            {
                throw new Exception("Missing 'Admin' role to remove role from user.");
            }

            string cypher = $"MATCH (u:User {{username: '{request.Username}'}}) ";
            cypher += $"MATCH (r:Role {{name: '{request.Role}'}}) ";
            cypher += $"MATCH (u)-[hr:HAS_ROLE]->(r) DELETE hr";
            ExecuteCypher(cypher, write: true);

            await SendSuccessResponseToClient(request.Info);
        }
    }
}
