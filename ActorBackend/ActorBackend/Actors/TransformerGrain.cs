using ActorBackend.Data;
using ActorBackend.Transformations;
using Proto;

namespace ActorBackend.Actors
{
    public class TransformerGrain : TransformerGrainBase
    {
        private Dictionary<string, List<ITransformStep>> transforms;
        private List<string> transformOrder;

        private List<string>? toRemove = null;

        public TransformerGrain(IContext context) : base(context)
        {
        }

        public override Task Create(TransformerGrainCreateInfo request)
        {
            var desc = TransformationDescription.FromSpecification(request.Transformations);

            var parseContext = new ParsingContext(request.NodeCollection);
            this.transforms = TransformationStepParser.Parse(desc, parseContext);

            transformOrder = desc.Changes.Select(ch => ch.StoredIn).ToList();

            toRemove = desc.RemoveNodes;

            return Task.CompletedTask;
        }

        public override Task<Data> TransformData(Data request)
        {
            transformOrder.ForEach(tr =>
            {
                try
                {
                    var ctx = new Transformations.ExecutionContext(request);
                    transforms[tr].ForEach(ts => ts.Execute(ctx));
                
                    if (request.Data_.ContainsKey(tr))
                        request.Data_[tr].Value = ctx.PrevValue;
                    else
                    {
                        request.Data_.Add(tr, new Data.Types.Node() { Value = ctx.PrevValue, DataType = ctx.PrevType});
                    }
                }
                catch(Exception)
                {

                }
            });

            if (toRemove != null)
            {
                toRemove.ForEach(tr => request.Data_.Remove(tr));
            }

            return Task.FromResult(request);
        }
    }
}
