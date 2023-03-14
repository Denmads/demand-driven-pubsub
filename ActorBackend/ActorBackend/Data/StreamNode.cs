﻿namespace ActorBackend.Data
{
    public class StreamNode
    {
        public string Topic { get; set; }
        public string DataType { get; set; }

        public override string? ToString()
        {
            return $"StreamNode{{Topic={Topic},DataType={DataType}}}";
        }
    }
}
