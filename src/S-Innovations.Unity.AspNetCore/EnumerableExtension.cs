﻿using Unity.Builder;
using Unity.Extension;
using Unity.Strategy;

namespace SInnovations.Unity.AspNetCore
{
    public class EnumerableExtension : UnityContainerExtension
    {
        protected override void Initialize()
        {
            // Enumerable strategy
            Context.Strategies.AddNew<EnumerableResolutionStrategy>(
                UnityBuildStage.TypeMapping);

        }
    }
}
