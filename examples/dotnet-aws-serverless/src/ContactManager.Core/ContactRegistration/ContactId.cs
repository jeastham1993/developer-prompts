namespace ContactManager.Core.ContactRegistration;

public readonly record struct ContactId(Guid Value)
{
    public static ContactId New() => new(Guid.NewGuid());
    
    public static ContactId Parse(string value) => new(Guid.Parse(value));
    
    public override string ToString() => Value.ToString();
}