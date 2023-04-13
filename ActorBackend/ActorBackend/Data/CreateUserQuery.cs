namespace ActorBackend.Data
{
    public class CreateUserQuery
    {
        public string ClientId { get; set; }
        public int RequestId { get; set; }

        //To Create
	    public string Username { get; set; }
        public string Password { get; set; } //Is encoded

        //Operator
        public string Account { get; set; }
        public string AccountPassword { get; set; } //Is encoded

    }
}
