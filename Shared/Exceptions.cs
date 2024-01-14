namespace SmartCarWebApp.Shared;

public class TokenExchangeException : Exception
{
    public TokenExchangeException(string message) : base(message) { }
}

public class VehicleListException : Exception
{
    public VehicleListException(string message) : base(message) { }
}

public class VehicleInfoException : Exception
{
    public VehicleInfoException(string message) : base(message) { }
}

public class LockOrUnlockException : Exception
{
    public LockOrUnlockException(string message) : base(message) { }
}
public class GetLockStatusException : Exception
{
    public GetLockStatusException(string message) : base(message) { }
}
