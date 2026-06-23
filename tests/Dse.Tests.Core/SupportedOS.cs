// Copyright (c) PNC Financial Services. All rights reserved.


namespace Dse.Tests;

// We're using an enum here, because we want the user to be able to pass the value to our attribute,
// and the existing OSPlatform structure is not appropriate for that. Plus, we like the name "macOS"
// better than the name "OSX". :)

public enum SupportedOs
{
    FreeBsd = 1,
    Linux = 2,
    MacOs = 3,
    Windows = 4,
}
