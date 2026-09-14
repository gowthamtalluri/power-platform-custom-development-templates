namespace Contoso.Crm.Plugins.Infrastructure
{
    /// <summary>Magic strings, centralised. Never inline "Target" in a handler.</summary>
    public static class ParameterNames
    {
        public const string Target = "Target";
        public const string Query = "Query";
        public const string BusinessEntity = "BusinessEntity";
        public const string BusinessEntityCollection = "BusinessEntityCollection";
        public const string Relationship = "Relationship";
        public const string RelatedEntities = "RelatedEntities";
        public const string State = "State";
        public const string Status = "Status";
        public const string Id = "Id";
    }

    public static class ImageNames
    {
        public const string PreImage = "PreImage";
        public const string PostImage = "PostImage";
    }

    public static class MessageNames
    {
        public const string Create = "Create";
        public const string Update = "Update";
        public const string Delete = "Delete";
        public const string Retrieve = "Retrieve";
        public const string RetrieveMultiple = "RetrieveMultiple";
        public const string Associate = "Associate";
        public const string Disassociate = "Disassociate";
        public const string SetStateDynamicEntity = "SetStateDynamicEntity";
        public const string Merge = "Merge";
        public const string Assign = "Assign";
    }

    public enum PipelineStage
    {
        PreValidation = 10,
        PreOperation = 20,
        MainOperation = 30,
        PostOperation = 40
    }
}
