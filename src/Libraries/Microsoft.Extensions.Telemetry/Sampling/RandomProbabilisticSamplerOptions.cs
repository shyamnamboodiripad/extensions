﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.Diagnostics.Sampling;

/// <summary>
/// The options for the Random Probabilistic sampler.
/// </summary>
public class RandomProbabilisticSamplerOptions
{
    /// <summary>
    /// Gets or sets the collection of <see cref="RandomProbabilisticSamplerFilterRule"/> used for filtering log messages.
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only - setter is necessary for options pattern
    [Required]
    [ValidateEnumeratedItems]
    public IList<RandomProbabilisticSamplerFilterRule> Rules { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
