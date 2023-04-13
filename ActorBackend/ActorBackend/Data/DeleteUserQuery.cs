﻿namespace ActorBackend.Data
{
    public class DeleteUserQuery
    {
        public string ClientId { get; set; }
        public int RequestId { get; set; }

        //To Create
	    public string Username { get; set; }

        //Operator
        public string Account { get; set; }
        public string AccountPassword { get; set; } //Is encoded

    }
}
