namespace Engine.Core.Exceptions;

public class WeaverDidNotRunException() : Exception(
    "The Weaver did not run on this assembly. Please ensure that the build process is correctly configured to include the Weaver step.")
{
    
}