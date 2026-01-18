using Microsoft.Extensions.VectorData;

namespace Agent.Console.Models
{
    public class PropertyVectorRecord
    {
        [VectorStoreKey]
        public Guid Id { get; set; }        
        [VectorStoreData] public string Title { get; set; }
        [VectorStoreData] public string Rooms { get; set; }
        [VectorStoreData] public string Status { get; set; }
        [VectorStoreData] public string Description { get; set; }
        [VectorStoreData] public List<string> NearbySchools { get; set; }
        [VectorStoreData] public string AgentName { get; set; }
        [VectorStoreData] public string Address { get; set; }
        
        [VectorStoreVector(1536, DistanceFunction = DistanceFunction.CosineDistance)]
        public string SearchDocument =>
        $"""
         Property listing.
         Title: {Title}.
         This property has {Rooms} rooms.
         Current status: {Status}.
         Located at {Address}.
         Description: {Description}.
         Nearby schools include: {string.Join(", ", NearbySchools)}.
         Listed by agent: {AgentName}.
        """;
    }

}
