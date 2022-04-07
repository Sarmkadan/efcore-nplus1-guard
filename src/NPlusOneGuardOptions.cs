// Copyright (c) 2023-present EF Core N+1 Guard contributors. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

#nullable enable
using System;

namespace EfCoreNPlusOneGuard
{
    public class NPlusOneGuardOptions
    {
        public int Threshold { get; set; } = 5;
        public TimeSpan DetectionWindow { get; set; } = TimeSpan.FromSeconds(2);
        public bool ThrowOnDetection { get; set; } = false;
        public bool LogOnDetection { get; set; } = true;
        public List<string> IgnoredQueryPatterns { get; set; } = new List<string>();
    }
}
