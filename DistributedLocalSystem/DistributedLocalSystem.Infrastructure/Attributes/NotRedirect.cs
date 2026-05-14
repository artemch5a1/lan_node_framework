namespace DistributedLocalSystem.Infrastructure.Attributes;

[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
public class NotRedirect : Attribute
{
    public NotRedirect() { }
}
