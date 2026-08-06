namespace Example.TestModels;

public class DatabaseModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActived { get; set; }

    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }

    public bool IsDeleted { get; set; }
}

public class Dto
{
    public int Id { get; set; }
    public string Name { get; set; }
    public bool IsActived { get; set; }

    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
