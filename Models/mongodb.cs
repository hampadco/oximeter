using MongoDB.Bson.Serialization.Attributes;

public class mongodb
{
    
    [BsonId]
    [BsonRepresentation(MongoDB.Bson.BsonType.ObjectId)]
    public string Id { get; set; }
    public int chart { get; set; }

    public int pulse { get; set; }

    public int spo { get; set; }

    public DateTime Datetime { get; set; }
    
    
    
    
}