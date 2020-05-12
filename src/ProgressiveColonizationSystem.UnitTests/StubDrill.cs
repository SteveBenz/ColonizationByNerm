﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubDrill
        : ITieredProducer
    {
        public const double DefaultProductionRate = 10.0;

        public TechTier Tier { get; set; } = TechTier.Tier0;
        public TechTier MaximumTier { get; set; } = TechTier.Tier4;
        public double ProductionRate { get; set; } = DefaultProductionRate;
        public bool IsResearchEnabled { get; set; } = true;
        public string ReasonWhyResearchIsDisabled { get; } = null;
        public bool IsProductionEnabled { get; set; } = true;
        public TieredResource Output => StubColonizationResearchScenario.Stuff;
        public TieredResource Input => null;
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            return target.ContributeResearch(this.Output, "test", amount);
        }
        public string Body { get; set; } = "test";
    }
}
