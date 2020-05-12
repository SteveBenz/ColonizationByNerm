﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProgressiveColonizationSystem
{
    public class IntervesselResourceTransfer
    {
        private double transferStartTime;
        private double expectedTransferCompleteTime;
        private double lastTransferTime;
        private ResourceConverter resourceConverter;
        private ConversionRecipe thisVesselConversionRecipe;
        private ConversionRecipe otherVesselConversionRecipe;
        private Vessel sourceVessel;

        private const double transferTimeInSeconds = 30;

        public bool IsTransferUnderway { get; private set; }

        public bool IsTransferComplete { get; private set; }

        public double TransferPercent
        {
            get
            {
                if (this.IsTransferComplete)
                {
                    return 1;
                }
                else if (this.IsTransferUnderway)
                {
                    return Math.Min(1, (Planetarium.GetUniversalTime() - transferStartTime) / (expectedTransferCompleteTime - transferStartTime));
                }
                else
                {
                    return 0;
                }
            }
        }

        public Vessel TargetVessel { get; set; }

        public void StartTransfer()
        {
            if (IsTransferUnderway)
            {
                return;
            }

            if (!TryFindResourceToTransfer(FlightGlobals.ActiveVessel, this.TargetVessel, out Dictionary<string,double> toSend, out Dictionary<string,double> toReceive))
            {
                return;
            }

            this.sourceVessel = FlightGlobals.ActiveVessel;
            this.resourceConverter = new ResourceConverter();
            this.thisVesselConversionRecipe = new ConversionRecipe();
            this.otherVesselConversionRecipe = new ConversionRecipe();
            this.lastTransferTime = transferStartTime = Planetarium.GetUniversalTime();
            this.expectedTransferCompleteTime = transferStartTime + transferTimeInSeconds;
            this.IsTransferUnderway = true;
            this.IsTransferComplete = false;
            foreach (string resource in toSend.Keys.Union(toReceive.Keys))
            {
                bool isSending;
                double amount;
                isSending = toSend.TryGetValue(resource, out amount);
                if (!isSending)
                {
                    amount = toReceive[resource];
                }

                double amountPerSecond = amount / transferTimeInSeconds;
                (isSending ? thisVesselConversionRecipe.Inputs : thisVesselConversionRecipe.Outputs)
                    .Add(new ResourceRatio(resource, amountPerSecond, dumpExcess: false));
                (isSending ? otherVesselConversionRecipe.Outputs : otherVesselConversionRecipe.Inputs)
                    .Add(new ResourceRatio(resource, amountPerSecond, dumpExcess: false));
            }
        }

        private void CheckIfSatisfiedAutoMiningRequirement()
        {
            foreach (var resource in this.thisVesselConversionRecipe.Outputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.sourceVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.TargetVessel, resource.Ratio * transferTimeInSeconds);
                    return;
                }
            }
            foreach (var resource in this.thisVesselConversionRecipe.Inputs)
            {
                if (this.IsResourceSatisfyingMiningRequirement(resource))
                {
                    this.TargetVessel.vesselModules.OfType<SnackConsumption>().First()
                        .MiningMissionFinished(this.sourceVessel, resource.Ratio * transferTimeInSeconds);
                    return;
                }
            }
        }

        private bool IsResourceSatisfyingMiningRequirement(ResourceRatio resource)
        {
            if (!ColonizationResearchScenario.Instance.TryParseTieredResourceName(resource.ResourceName, out TieredResource tieredResource, out TechTier tier)
             || tieredResource != ColonizationResearchScenario.Instance.CrushInsResource)
            {
                return false;
            }

            // TODO: Validate that the amount is sufficient;
            return true;
        }

        public void Reset()
        {
            this.sourceVessel = null;
            this.resourceConverter = null;
            this.otherVesselConversionRecipe = null;
            this.thisVesselConversionRecipe = null;
            this.IsTransferUnderway = false;
            this.IsTransferComplete = false;
        }

        public void OnFixedUpdate()
        {
            if (FlightGlobals.ActiveVessel.situation != Vessel.Situations.LANDED)
            {
                this.Reset();
                return;
            }

            if (IsTransferComplete && FlightGlobals.ActiveVessel == this.sourceVessel)
            {
                return;
            }

            if (IsTransferUnderway && FlightGlobals.ActiveVessel == this.sourceVessel)
            {
                double now = Planetarium.GetUniversalTime();
                double elapsedTime = now - this.lastTransferTime;
                this.lastTransferTime = now;

                // Move the goodies
                var thisShipResults = this.resourceConverter.ProcessRecipe(elapsedTime, this.thisVesselConversionRecipe, this.sourceVessel.rootPart, null, 1f);
                var otherShipResults = this.resourceConverter.ProcessRecipe(elapsedTime, this.otherVesselConversionRecipe, this.TargetVessel.rootPart, null, 1f);
                if (thisShipResults.TimeFactor < elapsedTime || otherShipResults.TimeFactor < elapsedTime)
                {
                    this.CheckIfSatisfiedAutoMiningRequirement();
                    this.resourceConverter = null;
                    this.thisVesselConversionRecipe = null;
                    this.otherVesselConversionRecipe = null;
                    this.IsTransferComplete = true;
                }

                return;
            }
        }

        private static HashSet<string> GetVesselsProducers(Vessel vessel)
        {
            HashSet<string> products = new HashSet<string>();

            foreach (var tieredConverter in vessel.FindPartModulesImplementing<PksTieredResourceConverter>())
            {
                products.Add(tieredConverter.Output.TieredName(tieredConverter.Tier));
            }

            foreach (var oldSchoolConverter in vessel.FindPartModulesImplementing<ModuleResourceConverter>())
            {
                foreach (var resourceRation in oldSchoolConverter.Recipe.Outputs)
                {
                    products.Add(resourceRation.ResourceName);
                }
            }

            foreach (var combiner in vessel.FindPartModulesImplementing<PksTieredCombiner>())
            {
                products.Add(combiner.untieredOutput);
            }
            return products;
        }

        private static HashSet<string> GetVesselsConsumers(Vessel vessel)
        {
            HashSet<string> consumers = new HashSet<string>();

            // None of the tiered resources really make sense to produce in one place and
            // process in another, but the combiners do require this sort of thing.
            foreach (var combiner in vessel.FindPartModulesImplementing<PksTieredCombiner>())
            {
                consumers.Add(combiner.untieredInput);
            }
            return consumers;
        }

        private enum SnacksDirection
        {
            Send,
            Receive,
            Neither,
        }

        public static bool TryFindResourceToTransfer(Vessel sourceVessel, Vessel otherVessel, out Dictionary<string, double> toSend, out Dictionary<string, double> toReceive)
        {
            SnackConsumption.ResourceQuantities(sourceVessel, 1, out Dictionary<string, double> thisShipCanSupply, out Dictionary<string, double> thisShipCanStore);
            SnackConsumption.ResourceQuantities(otherVessel, 1, out Dictionary<string, double> otherShipCanSupply, out Dictionary<string, double> otherShipCanStore);

            string[] unpassableStuff = new string[] { "ElectricCharge", "StoredCharge" };
            foreach (var stuff in unpassableStuff)
            {
                foreach (var dict in new[] { thisShipCanSupply, thisShipCanStore, otherShipCanStore, otherShipCanSupply })
                {
                    dict.Remove(stuff);
                }
            }

            List<string> couldSend = thisShipCanSupply.Keys.Intersect(otherShipCanStore.Keys).ToList();
            List<string> couldTake = otherShipCanSupply.Keys.Intersect(thisShipCanStore.Keys).ToList();

            List<PksTieredResourceConverter> otherVesselProducers = otherVessel.FindPartModulesImplementing<PksTieredResourceConverter>();

            // Refactor me!  Way too much "if other vessel has quality, then send, else if this vessel has that quality, receive."

            toSend = new Dictionary<string, double>();
            toReceive = new Dictionary<string, double>();
            if ((otherVessel.GetCrewCount() == 0 && otherVesselProducers.Count > 0)
                || otherVessel.vesselType == VesselType.Debris)
            {
                // The player's trying to abandon the base so we'll take everything and give nothing.
                foreach (var otherShipPair in otherShipCanSupply)
                {
                    string resourceName = otherShipPair.Key;
                    if (thisShipCanStore.ContainsKey(resourceName))
                    {
                        toReceive.Add(resourceName, Math.Min(thisShipCanStore[resourceName], otherShipPair.Value));
                    }
                }
                return toReceive.Count > 0;
            }

            // If other ship has a producer for a resource, take it
            // and if this ship has a producer for a resource (and the other doesn't), give it
            HashSet<string> thisShipsProducts = GetVesselsProducers(sourceVessel);
            HashSet<string> otherShipsProducts = GetVesselsProducers(otherVessel);
            foreach (string takeableStuff in otherShipCanSupply.Keys.Union(thisShipCanSupply.Keys))
            {
                if (otherShipsProducts.Contains(takeableStuff)
                 && !thisShipsProducts.Contains(takeableStuff)
                 && thisShipCanStore.ContainsKey(takeableStuff)
                 && otherShipCanSupply.ContainsKey(takeableStuff))
                {
                    toReceive.Add(takeableStuff, Math.Min(thisShipCanStore[takeableStuff], otherShipCanSupply[takeableStuff]));
                }
                else if (!otherShipsProducts.Contains(takeableStuff)
                       && thisShipsProducts.Contains(takeableStuff)
                       && otherShipCanStore.ContainsKey(takeableStuff)
                       && thisShipCanSupply.ContainsKey(takeableStuff))
                {
                    toSend.Add(takeableStuff, Math.Min(otherShipCanStore[takeableStuff], thisShipCanSupply[takeableStuff]));
                }
            }

            // If one ship needs a resource to produce stuff and the other doesn't, but has some in storage, move it.
            HashSet<string> thisShipsConsumption = GetVesselsConsumers(sourceVessel);
            HashSet<string> otherShipsConsumption = GetVesselsConsumers(otherVessel);
            foreach (string resourceName in thisShipsConsumption.Union(otherShipsConsumption))
            {
                if (otherShipCanSupply.ContainsKey(resourceName)
                 && thisShipCanStore.ContainsKey(resourceName)
                 && thisShipsConsumption.Contains(resourceName)
                 && !otherShipsConsumption.Contains(resourceName))
                {
                    toReceive.Add(resourceName, Math.Min(thisShipCanStore[resourceName], otherShipCanSupply[resourceName]));
                }
                else if (thisShipCanSupply.ContainsKey(resourceName)
                      && otherShipCanStore.ContainsKey(resourceName)
                      && otherShipsConsumption.Contains(resourceName)
                      && !thisShipsConsumption.Contains(resourceName))
                {
                    toSend.Add(resourceName, Math.Min(otherShipCanStore[resourceName], thisShipCanSupply[resourceName]));
                }
            }

            SnacksDirection snackDirectionBasedOnVesselType;
            if (sourceVessel.vesselType == VesselType.Ship && (otherVessel.vesselType == VesselType.Base || otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander)
             || sourceVessel.vesselType == VesselType.Base && (otherVessel.vesselType == VesselType.Rover || otherVessel.vesselType == VesselType.Lander))
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Send;
            }
            else if (otherVessel.vesselType == VesselType.Ship && (sourceVessel.vesselType == VesselType.Base || sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander)
                  || otherVessel.vesselType == VesselType.Base && (sourceVessel.vesselType == VesselType.Rover || sourceVessel.vesselType == VesselType.Lander))
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Receive;
            }
            else if (otherVessel.GetCrewCount() > 0 && sourceVessel.GetCrewCount() == 0)
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Send;
            }
            else if (otherVessel.GetCrewCount() == 0 && sourceVessel.GetCrewCount() > 0)
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Receive;
            }
            else
            {
                snackDirectionBasedOnVesselType = SnacksDirection.Neither;
            }

            // Send snacks?
            if (otherShipCanStore.ContainsKey("Snacks-Tier4")
             && thisShipCanSupply.ContainsKey("Snacks-Tier4")
             && (thisShipsProducts.Contains("Snacks-Tier4")   // Always send if we produce it
              || (!otherShipsProducts.Contains("Snacks-Tier4") // Don't send if the other guy produces
               && snackDirectionBasedOnVesselType == SnacksDirection.Send)))
            {
                toReceive.Remove("Snacks-Tier4");
                toSend["Snacks-Tier4"] = Math.Min(thisShipCanSupply["Snacks-Tier4"], otherShipCanStore["Snacks-Tier4"]);
            }
            else if (thisShipCanStore.ContainsKey("Snacks-Tier4")
                  && otherShipCanSupply.ContainsKey("Snacks-Tier4")
                  && (otherShipsProducts.Contains("Snacks-Tier4")   // Always take if the other guy produces
                   || (!thisShipsProducts.Contains("Snacks-Tier4")  // Don't take if we produce it
                    && snackDirectionBasedOnVesselType == SnacksDirection.Receive)))
            {
                toSend.Remove("Snacks-Tier4");
                toReceive["Snacks-Tier4"] = Math.Min(thisShipCanStore["Snacks-Tier4"], otherShipCanSupply["Snacks-Tier4"]);
            }

            return toReceive.Any() || toSend.Any();
        }
    }
}
