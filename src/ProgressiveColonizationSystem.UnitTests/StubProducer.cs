﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgressiveColonizationSystem.UnitTests
{
    public class StubProducer
        : ITieredProducer
    {
        public StubProducer(TieredResource output, TieredResource input, double productionRate, TechTier tier)
        {
            this.Output = output;
            this.Input = input;
            this.ProductionRate = productionRate;
            this.Tier = tier;
            this.Body = (output.ProductionRestriction == ProductionRestriction.Space) ? null : "munmuss";
        }

        public TechTier Tier { get; set; }
        public TechTier MaximumTier { get; set; } = TechTier.Tier4;
        public double ProductionRate { get; set; }
        public bool IsResearchEnabled { get; set; } = true;
        public bool IsProductionEnabled { get; set; } = true;
        public string ReasonWhyResearchIsDisabled { get; set; }
        public TieredResource Output { get; }
        public TieredResource Input { get; }
        public bool ContributeResearch(IColonizationResearchScenario target, double amount)
        {
            return target.ContributeResearch(this.Output, this.Body, amount);
        }
        public string Body { get; set; }
    }
}
