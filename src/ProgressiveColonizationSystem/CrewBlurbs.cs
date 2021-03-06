﻿using KSP.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using static LingoonaGrammarExtensions;

namespace ProgressiveColonizationSystem
{
    public static class CrewBlurbs
    {
        internal static System.Random random = new System.Random();

        private static List<CrewDescriptor> GetCrewDescriptors(Func<ProtoCrewMember, bool> isInstrumental)
            => FlightGlobals.ActiveVessel.GetVesselCrew().Select(c => FromKsp(c, isInstrumental)).ToList();

        internal static string HungryKerbals(List<ProtoCrewMember> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
            => HungryKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList(), daysToGrouchy, anyFoodProduction);

        internal static string HungryKerbals(List<CrewDescriptor> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
        {
            if (anyFoodProduction)
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} can't make any snacks!  {Capitalize(heshethey(crewInBucket))} can scrounge up old pizza crusts for {(int)(daysToGrouchy + .5)} more days.");
            }
            else
            {
                return $"{GetGroupDescription(crewInBucket)} {isare(crewInBucket)} surviving off a small stash of snacks; {heshethey(crewInBucket)} will be okay for {(int)(daysToGrouchy + .5)} more days.";
            }
        }

        internal static string GrumpyKerbals(List<ProtoCrewMember> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
            => GrumpyKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList(), daysToGrouchy, anyFoodProduction);

        internal static string GrumpyKerbals(List<CrewDescriptor> crewInBucket, double daysToGrouchy, bool anyFoodProduction)
        {
            if (anyFoodProduction)
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} can't make any snacks!  {Capitalize(heshethey(crewInBucket))} are starting to spend more and more time drawing up legal action against KSP than they are working.");
            }
            else
            {
                return Yellow($"{GetGroupDescription(crewInBucket)} can't find any more food!  {Capitalize(heshethey(crewInBucket))} need to get home soon!");
            }
        }

        internal static string StarvingKerbals(List<ProtoCrewMember> crewInBucket)
            => StarvingKerbals(crewInBucket.Select(k => FromKsp(k, _ => false)).ToList());

        internal static string StarvingKerbals(List<CrewDescriptor> crewInBucket)
        {
            return TextEffects.Red($"{GetGroupDescription(crewInBucket)} {isare(crewInBucket)} refusing to do any more work and {isare(crewInBucket)} contemplating legal action against KSP!  Get {himherthem(crewInBucket)} home or get some food out here right away!");
        }

        //  /* RegexOptions.Compiled /* | RegexOptions.CultureInvariant */
        private static readonly Regex replacement = new Regex(@"\[\w+\]");

        public static string CreateMessage(string baseMessageTag, List<ProtoCrewMember> crew, IEnumerable<string> experienceEffects, TechTier tier)
        {
            HashSet<string> possibleTraits = new HashSet<string>(
                experienceEffects.SelectMany(effect => GameDatabase.Instance
                    .ExperienceConfigs
                    .GetTraitsWithEffect(effect)));
            var crewDescriptors = crew.Select(c => FromKsp(c, protocrew => possibleTraits.Contains(protocrew.trait))).ToList();
            CrewDescriptor perp = null;
            CrewDescriptor victim = null;

            string Replacer(Match match)
            {
                switch (match.Value)
                {
                    case "[tier]":
                        return tier.DisplayName();
                    case "[perp_name]":
                        perp = perp ?? ChoosePerpetrator(crewDescriptors);
                        return perp.Name;
                    case "[victim_name]":
                        victim = victim ?? ChooseVictim(crewDescriptors);
                        return victim.Name;
                    case "[perp_heshe]":
                        perp = perp ?? ChoosePerpetrator(crewDescriptors);
                        return perp.heshe;
                    case "[perp_himher]":
                        perp = perp ?? ChoosePerpetrator(crewDescriptors);
                        return perp.himher;
                    case "[perp_hisher]":
                        perp = perp ?? ChoosePerpetrator(crewDescriptors);
                        return perp.hisher;
                    case "[victim_hisher]":
                        victim = victim ?? ChooseVictim(crewDescriptors);
                        return victim.hisher;
                    case "[perps]":
                        return GetGroupDescription(crewDescriptors, true);
                    case "[victims]":
                        return GetGroupDescription(crewDescriptors, false);
                    case "[victims_hashave]":
                        return crewDescriptors.hashave();
                    case "[victims_isare]":
                        return crewDescriptors.isare();
                    case "[victims_hishertheir]":
                        return crewDescriptors.hishertheir();
                    case "[crew]":
                        return GetGroupDescription(crewDescriptors);
                    case "[resource_name]":
                        return GetMessage("LOC_KPBS_RANDOM_MINERAL");
                    case "[body]":
                        return FlightGlobals.ActiveVessel.mainBody.name;
                    default:
                        return $"?Unknown Substitution: {match.Value}?";
                }
            }

            string message = replacement.Replace(GetMessage(baseMessageTag), Replacer);
            return message;
        }

        private static CrewDescriptor FromKsp(ProtoCrewMember kspCrew, Func<ProtoCrewMember, bool> isInstrumental)
            => new CrewDescriptor()
            {
                Name = FamiliarName(kspCrew),
                Gender = (Gender)kspCrew.gender, // <- works out (probably by design) that the KSP gender enum mappable in this way.
                        IsInstrumental = isInstrumental(kspCrew),
                IsBadass = kspCrew.isBadass
            };

        private static string FamiliarName(ProtoCrewMember kspCrew)
            => kspCrew.name.EndsWith(" Kerman") ? kspCrew.name.Substring(0, kspCrew.name.Length - 7) : kspCrew.name;

        public static string  Yellow(string s)
            => $"<color #ffff00>{s}</color>";

        internal static string heshethey(List<CrewDescriptor> l)
            => (l.Count > 1) ? "they" : l[0].heshe;

        internal static string himherthem(List<CrewDescriptor> l)
            => (l.Count > 1) ? "them" : l[0].himher;

        internal static string isare(List<CrewDescriptor> l)
            => (l.Count == 1) ? "is" : "are";

        internal static string Capitalize(string word)
            => $"{char.ToUpper(word[0])}{word.Substring(1)}";


        private static CrewDescriptor ChoosePerpetrator(List<CrewDescriptor> crew)
            => ChooseOne(crew, true);

        private static CrewDescriptor ChooseVictim(List<CrewDescriptor> crew)
            => ChooseOne(crew, false);

        private static CrewDescriptor ChooseOne(List<CrewDescriptor> crew, bool? isInstrumental)
        {
            // Try to find a badass
            if (random.Next(10) > 0)
            {
                List<CrewDescriptor> badAssPerps = crew.Where(c => c.IsBadass && (!isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value)).ToList();
                if (badAssPerps.Count > 0)
                {
                    return badAssPerps[random.Next(badAssPerps.Count)];
                }
            }

            List<CrewDescriptor> allPerps = crew.Where(c => !isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value).ToList();
            return allPerps[random.Next(allPerps.Count)];
        }

        private static string GetGroupDescription(List<CrewDescriptor> crew, bool? isInstrumental)
            => GetGroupDescription(crew.Where(c => !isInstrumental.HasValue || c.IsInstrumental == isInstrumental.Value).ToList());

        private static string GetGroupDescription(List<CrewDescriptor> crew)
        {
            if (crew.Count == 1)
            {
                return crew[0].Name;
            }

            CrewDescriptor perp = ChooseOne(crew, null);
            List<CrewDescriptor> gang = crew.Where(c => c != perp).ToList();
            if (crew.Count == 2)
            {
                return $"{perp.Name} and {gang[0].Name}";
            }
            else if (gang.All(c => c.Gender == Gender.Male))
            {
                return $"{perp.Name} and the boys";
            }
            else if (gang.All(c => c.Gender == Gender.Female))
            {
                return $"{perp.Name} and the gals";
            }
            else
            {
                return $"{perp.Name} and the gang";
            }
        }

        private static string GetMessage(string messageTagPrefix)
        {
            if (!messageTagPrefix.StartsWith("#"))
            {
                messageTagPrefix = "#" + messageTagPrefix;
            }

            if (Localizer.TryGetStringByTag(messageTagPrefix, out var exactMessage))
            {
                return exactMessage;
            }

            int i = 0;
            List<string> possibleMessages = new List<string>();
            while (Localizer.TryGetStringByTag($"{messageTagPrefix}_{i}", out var indexedMessage))
            {
                possibleMessages.Add(indexedMessage);
                ++i;
            }

            if (i == 0)
            {
                Debug.LogError($"Missing localized message #{messageTagPrefix}");
                return $"!Missing Message: #{messageTagPrefix}!";
            }

            return possibleMessages[CrewBlurbs.random.Next(i)];
        }
    }

    public static class CrewBlurbExtensions
    {
        public static string hashave(this List<CrewDescriptor> crewDescriptors)
            => crewDescriptors.Count > 1 ? "have" : "has";
        public static string isare(this List<CrewDescriptor> crewDescriptors)
            => crewDescriptors.Count > 1 ? "are" : "is";
        public static string hishertheir(this List<CrewDescriptor> crewDescriptors)
            => crewDescriptors.Count == 1 ? crewDescriptors[0].hisher : "their";
    }


    public class CrewDescriptor
    {
        public string Name;

        public Gender Gender;

        /// <summary>
        ///   True if the crew member is somehow instrumental in what's happening
        /// </summary>
        public bool IsInstrumental;

        /// <summary>
        ///   True if this is a well-known kerbal
        /// </summary>
        public bool IsBadass;

        public string hisher
            => Gender == Gender.Male ? "his" : "her";

        public string heshe
            => Gender == Gender.Male ? "he" : "she";

        public string himher
            => Gender == Gender.Male ? "him" : "her";
    }
}
