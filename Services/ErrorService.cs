using System;

namespace NexusLauncher.Services;

public class ErrorService
{
    public string FormatError(Exception ex) => ex.Message;
}
