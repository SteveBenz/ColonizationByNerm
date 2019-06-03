# Progressive Colonization System Work List

Because I can't be bothered with GitHub issues.

1. Bugs:
   1.  Resource transfer seems not to work for some ships -- perhaps because of staging
   2.  (fixed) Can bring CrushIns from Kerbin or other bodies and fake out the resource auto-collection mechanic.  It should be happy
       to take the crushins, but disallow it as an auto-collector.
   3.  If there's more than one manned base, it should ask you where you want to look for crushins
   4.  Something seems to reset the dialog box to center from time to time
   5.  Maybe it's not consuming the crushins out of the ground.
   6.  (fixed) K&K Workshop part is not in the PKS tab
   7.  Messy text when kerbals run out of food
   8.  No message outside of cupcake dialog when kerbals first run out of food
   9.  (fixed) Doesn't tell you that your ship has no food when you have parts that require kerbals and no kerbals
   10. (fixed) If you just set the body without setting the tier, it sets the parts to Tier 4
   11. (fixed) Crew dialog is busted in the SPH -- thinks all the parts are disabled and doesn't show any crew
   12. (fixed) Complains about a lack of fertilizer when T4 fertilizer is present and there's a Hydroponics producer
   13. (fixed) Crew dialog does not update when kerbals are added in the Editor
   14. (fixed) On Progression pane, "Production" comes up twice at Mun Base 1
   15. (fixed) Tooltips apparently don't get line-wrapped.
   16. (fixed) Scan for Loose Crushins shouldn't be visible below T2
   17. If there's no Complex Parts, it doesn't list that as a limiter in the in-flight production tab.
   18. If there's no complex parts, it doesn't list that as a problem in the editor warnings tab.
       ^-- both of these might be due to it not showing any indicator if you don't have storage for the output.
   19. (fixed) Stuff Scanner part is showing up under Science tab
2. Rounding things out
   1.  Create or hijack a part for storing Snacks - just snacks
   2.  (complete) Write PDF help and or create a github wiki
   3.  (complete) Make the shinies boost your rep on landing - remember to add checks to make sure Shinies don't actually come from Kerbin
   4.  Get a better scanner part
   5.  Fully automate release preparation
   6.  Add .version to output per guidance: https://github.com/KSP-CKAN/NetKAN/pull/7147
       ^-- added the version file, but CKAN doesn't know about it yet.  Fix it at the next version update.
   7.  Maybe allow some warnings to show a "Don't care" button (Particularly the storage ones)
   8.  (DOA) Make a "K&K Storage [KIS]" that works with B9 -- need to figure out how to do the decals and push it to K&K
       Mod author has other ideas.
3. Things that don't seem worth doing
   1.  Integrate with Kerbal Alarm Clock?
   2.  IDEA: Make it so that having achieved Tier-X farming on one world makes Tier-X-1 easier to achieve on a new world.
   3.  Delete the parts supporting EL's production chain
     