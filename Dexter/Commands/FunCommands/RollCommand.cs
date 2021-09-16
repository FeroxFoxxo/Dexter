using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Configurations;
using Dexter.Extensions;
using Dexter.Helpers;
using Discord.Commands;
using System.Text;
using System.Text.RegularExpressions;

namespace Dexter.Commands
{
    public partial class FunCommands
    {

        /// <summary>
        /// Handles rolling dice of different types, as well as certain useful output transformations.
        /// </summary>
        /// <param name="roll"></param>
        /// <returns></returns>

        [Command("roll")]
        [Summary("Rolls one or more dice! Example rolls: rolling a 6-sided die: `d6`, rolling two twenty-sided dice: `2d20`")]
        [ExtendedSummary("Rolls one or more dice!\n" +
            "Basic structure: `(number)d[faces]`, to roll (number) [faces]-faced dice. (use an uppercase D to make rolls verbose.)\n" +
            "**Modifiers**: Modifiers can be appended to the roll separated by spaces, they modify the results:\n" +
            "- +[n], -[n], +[n]d[D], -[n]d[D] - Adds or subtracts a number or a series of rolls.\n" +
            "- kh[n], kl[n], dh[n], dl[n] - Keep or drop highest or lowest [n] rolls. e.g. {4, 1, 3} => (**kh2**) {4, 3}.\n" +
            "- rr[n], rr>[n], rr<[n] - Reroll any dice whose roll value satisfies the condition. {4, 1, 3} => (**rr1**) => {4, 2, 3}.\n" +
            "- [n]=>[m] - Replaces a number with another for all rolled n's.\n" +
            "- !(n), !>[n], !<[n] - Explode once if a condition is satisfied. {4, 1, 3} => (**!>2**) {4, 1, 3, 2, 1}.\n" +
            "- !!(n)(,max), !!>(n)(,max), !!<n(,max) - Explode up to max times if a condition is satisfied. {4, 1, 3} => (!!>2,3) => {4, 1, 3, 2, 3, 4}")]
        [CommandCooldown(10)]

        public async Task RollCommand([Remainder] string roll)
        {

            string baseRoll = Regex.Match(roll, @"^([1-9][0-9]*)?[dD][1-9][0-9]*").Value;
            if (string.IsNullOrEmpty(baseRoll))
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Invalid roll!")
                    .WithDescription($"Please follow a valid roll expression. Type `{BotConfiguration.Prefix}help roll` for more information.")
                    .SendEmbed(Context.Channel);
                return;
            }
            int dIndex = baseRoll.IndexOf("d");
            if (dIndex < 0) dIndex = baseRoll.IndexOf("D");
            bool verbose = baseRoll[dIndex] == 'D';

            if (!int.TryParse(baseRoll[..dIndex], out int n))
            {
                n = 1;
            }
            else if (n > FunConfiguration.MaxDieRolls)
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Excessive Roll Count!")
                    .WithDescription($"{n} is too high! Keep the number of dice to roll below {FunConfiguration.MaxDieRolls}.")
                    .SendEmbed(Context.Channel);
                return;
            }
            if (!int.TryParse(baseRoll[(dIndex + 1)..], out int d))
            {
                await BuildEmbed(Enums.EmojiEnum.Annoyed)
                    .WithTitle("Invalid Face Count!")
                    .WithDescription($"You entered number \"{baseRoll[(dIndex + 1)..]}\", make sure you use an input that is compatible with a 32-bit integer. (<{int.MaxValue})")
                    .SendEmbed(Context.Channel);
                return;
            }
            RollContext rollContext = new(n, d, new Random(), FunConfiguration);
            string[] rollArgs = roll.SanitizeMentions().Split(' ')[1..];

            List<string> report = new();
            List<int> rolls = rollContext.Roll();
            report.Add(rollContext.StringifyRolls("Base Rolls", rolls));

            List<string> errors = new();
            foreach (string rawmod in rollArgs)
            {
                bool success = false;
                foreach (KeyValuePair<string, Func<List<int>, string, RollContext, List<int>>> pair in Modifiers)
                {
                    string mod = Regex.Match(rawmod, @$"^{pair.Key}$").Value;
                    if (string.IsNullOrEmpty(mod)) continue;

                    rolls = pair.Value(rolls, mod, rollContext);
                    if (rolls.Count > FunConfiguration.MaxDieRolls)
                    {
                        await BuildEmbed(Enums.EmojiEnum.Annoyed)
                            .WithTitle("Modifier caused excess of maximum allowed rolls!")
                            .WithDescription($"The following modifier: `{mod}`, caused rolls to exceed {FunConfiguration.MaxDieRolls}, with a total of {rolls.Count} rolls.")
                            .SendEmbed(Context.Channel);
                        return;
                    }
                    report.Add(rollContext.StringifyRolls(mod, rolls));
                    rollContext.ResetHighlights();
                    success = true;
                    break;
                }

                if (!success)
                {
                    errors.Add(rawmod);
                }
            }

            string message;
            if (!verbose)
            {
                message = $"You rolled **{rolls.Sum()}**!";
            }
            else if (report.Count <= FunConfiguration.MaxDieRollExpressionCount)
            {
                message = string.Join('\n', report);
            }
            else
            {
                message = $"{string.Join('\n', report.ToArray()[..FunConfiguration.MaxDieRollExpressionCount])} => ... => \n{report.Last()}";
            }

            await Context.Channel.SendMessageAsync(message + (errors.Count > 0 ? $"\n**Invalid modifier expressions**: {string.Join(", ", errors)}" : ""));
        }



        private class RollContext
        {
            public int d;
            public int n;
            public Random rand;
            public FunConfiguration funConfiguration;

            public int underlineMin = -1;
            public int underlineMax = -1;
            public int italicizeMin = -1;
            public int italicizeMax = -1;
            public int highlightMin = -1;
            public int highlightMax = -1;

            public RollContext(int n, int d, Random rand, FunConfiguration funConfiguration)
            {
                this.n = n;
                this.d = d;
                this.rand = rand;
                this.funConfiguration = funConfiguration;
                ResetHighlights();
            }

            public List<int> Roll()
            {
                List<int> result = new(n);
                for (int i = 0; i < n; i++)
                {
                    result.Add(rand.Next(1, d + 1));
                }
                return result;
            }

            public string StringifyRolls(string title, List<int> rolls)
            {
                StringBuilder sb = new();
                foreach (int r in rolls)
                {
                    if (sb.Length > 0) sb.Append(", ");
                    sb.Append(StringifyRoll(r));
                    if (sb.LengthSpecial(LanguageHelper.DiscordRichTextChars) > funConfiguration.MaxDieRollExpressionLength)
                    {
                        sb.Append("...");
                        break;
                    };
                }
                string total;
                try
                {
                    total = rolls.Sum().ToString();
                }
                catch (OverflowException)
                {
                    total = $"Infinity (>{int.MaxValue})";
                }
                catch (Exception e)
                {
                    total = $"Error: {e.Message}";
                }
                return $"(**{title}**) {{{sb}}} = **{total}**";
            }

            public void ResetHighlights()
            {
                underlineMin = -1;
                underlineMax = -1;
                italicizeMin = -1;
                italicizeMax = -1;
                highlightMin = d;
                highlightMax = d;
            }

            private string StringifyRoll(int roll)
            {
                StringBuilder b = new();
                if (roll >= underlineMin && roll <= underlineMax) b.Append("__");
                if (roll >= italicizeMin && roll <= italicizeMax) b.Append('*');
                if (roll >= highlightMin && roll <= highlightMax) b.Append("**");
                b.Append(roll);
                if (roll >= highlightMin && roll <= highlightMax) b.Append("**");
                if (roll >= italicizeMin && roll <= italicizeMax) b.Append('*');
                if (roll >= underlineMin && roll <= underlineMax) b.Append("__");
                return b.ToString();
            }
        }

        private readonly Dictionary<string, Func<List<int>, string, RollContext, List<int>>> Modifiers = new()
        {
            {
                //Add a number
                @"\+[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int n = int.Parse(mod[1..]);
                    r.Add(n);
                    return r;
                }
            },
            {
                //Subtract a number
                @"-[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int n = int.Parse(mod[1..]);
                    r.Add(-n);
                    return r;
                }
            },
            {
                //Add a roll
                @"\+[0-9]{1,10}[dD][0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int di = mod.IndexOf('d');
                    if (di < 0) di = mod.IndexOf('D');
                    int n = int.Parse(mod[1..di]);
                    int d = int.Parse(mod[(di + 1)..]);
                    if (n > context.funConfiguration.MaxDieRolls - r.Count)
                        n = context.funConfiguration.MaxDieRolls - r.Count;

                    for (int i = 0; i < n; i++)
                    {
                        r.Add(context.rand.Next(1, d + 1));
                    }
                    return r;
                }
            },
            {
                //Subtract a roll
                @"-[0-9]{1,10}[dD][0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int di = mod.IndexOf('d');
                    if (di < 0) di = mod.IndexOf('D');
                    int n = int.Parse(mod[1..di]);
                    int d = int.Parse(mod[(di + 1)..]);
                    if (n > context.funConfiguration.MaxDieRolls - r.Count)
                        n = context.funConfiguration.MaxDieRolls - r.Count;

                    for (int i = 0; i < n; i++)
                    {
                        r.Add(-context.rand.Next(1, d + 1));
                    }
                    return r;
                }
            },
            {
                //Keep highest
                @"kh[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toKeep = int.Parse(mod[2..]);
                    if (toKeep >= r.Count) return r;
                    r.Sort();
                    List<int> result = new();
                    for (int i = 0; i < toKeep; i++)
                    {
                        result.Add(r[^(i + 1)]);
                    }
                    return result;
                }
            },
            {
                //Keep lowest
                @"kl[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toKeep = int.Parse(mod[2..]);
                    if (toKeep >= r.Count) return r;
                    r.Sort();
                    List<int> result = new();
                    for (int i = 0; i < toKeep; i++)
                    {
                        result.Add(r[i]);
                    }
                    return result;
                }
            },
            {
                //Drop highest
                @"dh[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toDrop = int.Parse(mod[2..]);
                    if (toDrop >= r.Count) return new List<int>();
                    r.Sort();
                    List<int> result = new();
                    for (int i = 0; i < r.Count - toDrop; i++)
                    {
                        result.Add(r[i]);
                    }
                    return result;
                }
            },
            {
                //Drop lowest
                @"dl[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toDrop = int.Parse(mod[2..]);
                    if (toDrop >= r.Count) return new List<int>();
                    r.Sort();
                    List<int> result = new();
                    for (int i = 0; i < r.Count - toDrop; i++)
                    {
                        result.Add(r[^(i + 1)]);
                    }
                    return result;
                }
            },
            {
                //Reroll
                @"rr[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    if (context.d == 1) return r;
                    int toReroll = int.Parse(mod[2..]);
                    for (int i = 0; i < r.Count; i++)
                    {
                        if (r[i] == toReroll)
                        {
                            r[i] = context.rand.Next(1, context.d);
                            if (r[i] >= toReroll) r[i]++;
                        }
                    }
                    return r;
                }
            },
            {
                //Reroll less than
                @"rr<[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toReroll = int.Parse(mod[3..]);
                    if (context.d < toReroll) return r;
                    for (int i = 0; i < r.Count; i++)
                    {
                        if (r[i] < toReroll)
                        {
                            r[i] = context.rand.Next(toReroll, context.d + 1);
                        }
                    }
                    return r;
                }
            },
            {
                //Reroll greater than
                @"rr>[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toReroll = int.Parse(mod[3..]);
                    if (toReroll < 1) return r;
                    for (int i = 0; i < r.Count; i++)
                    {
                        if (r[i] > toReroll)
                        {
                            r[i] = context.rand.Next(1, toReroll + 1);
                            if (r[i] >= toReroll) r[i]++;
                        }
                    }
                    return r;
                }
            },
            {
                //Replace
                @"[0-9]{1,10}=>[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    int toSwap = int.Parse(mod[..mod.IndexOf('=')]);
                    int swapTo = int.Parse(mod[(mod.IndexOf('>') + 1)..]);
                    for (int i = 0; i < r.Count; i++)
                    {
                        if (r[i] == toSwap) r[i] = swapTo;
                    }
                    return r;
                }
            },
            {
                //Explode
                @"!!([0-9]{1,10}|$)(,[0-9]{1,10})?",
                (List<int> r, string mod, RollContext context) =>
                {
                    if (context.d == 1) return r;
                    int maxSeparator = mod.IndexOf(',');
                    int toExplode;
                    int max = int.MaxValue;
                    if (maxSeparator < 0)
                    {
                        toExplode = mod.Length == 2 ? context.d : int.Parse(mod[2..]);
                    }
                    else
                    {
                        string toExplodeStr = mod[2..maxSeparator];
                        if (toExplodeStr.Length == 0) toExplode = context.d;
                        else toExplode = int.Parse(toExplodeStr);
                        max = int.Parse(mod[(maxSeparator + 1)..]);
                    }

                    context.highlightMin = context.highlightMax = toExplode;

                    int i = 0;
                    int rerolls = 0;
                    while (i < r.Count && rerolls < max)
                    {
                        if (r[i++] == toExplode)
                        {
                            r.Add(context.rand.Next(1, context.d + 1));
                            rerolls++;
                        }
                    }
                    return r;
                }
            },
            {
                //Explode Compare
                @"!!(<|>)[0-9]{1,10}(,[0-9]{1,10})?",
                (List<int> r, string mod, RollContext context) =>
                {
                    if (context.d == 1) return r;
                    bool explodeLower = mod[1] == '<';
                    int maxSeparator = mod.IndexOf(',');
                    int toExplode;
                    int max = int.MaxValue;
                    if (maxSeparator < 0)
                    {
                        toExplode = int.Parse(mod[3..]);
                    }
                    else
                    {
                        toExplode = int.Parse(mod[3..maxSeparator]);
                        max = int.Parse(mod[(maxSeparator + 1)..]);
                    }

                    if (explodeLower)
                    {
                        context.highlightMax = toExplode - 1;
                        context.highlightMin = 0;
                    }
                    else
                    {
                        context.highlightMax = context.d;
                        context.highlightMin = toExplode + 1;
                    }

                    if (max > context.funConfiguration.MaxDieRollExplosions) max = context.funConfiguration.MaxDieRollExplosions;

                    int i = 0;
                    int rerolls = 0;
                    while (i < r.Count && rerolls < max)
                    {
                        if (r[i] != toExplode && (r[i] > toExplode ^ explodeLower))
                        {
                            r.Add(context.rand.Next(1, context.d + 1));
                            rerolls++;
                        }
                        i++;
                    }
                    return r;
                }
            },
            {
                //Explode once
                @"!([1-9]|$)[0-9]{0,9}",
                (List<int> r, string mod, RollContext context) =>
                {
                    if (context.d == 1) return r;
                    int toExplode;
                    int max = context.funConfiguration.MaxDieRollExplosions;
                    toExplode = mod.Length == 1 ? context.d : int.Parse(mod[1..]);

                    context.highlightMin = context.highlightMax = toExplode;

                    int originalCount = r.Count;
                    int i = 0;
                    int rerolls = 0;
                    while (i < originalCount && rerolls < max)
                    {
                        if (r[i++] == toExplode)
                        {
                            r.Add(context.rand.Next(1, context.d + 1));
                            rerolls++;
                        }
                    }
                    return r;
                }
            },
            {
                //Explode once compare
                @"!(<|>)[0-9]{1,10}",
                (List<int> r, string mod, RollContext context) =>
                {
                    bool explodeLower = mod[1] == '<';
                    int toExplode;
                    int max = context.funConfiguration.MaxDieRollExplosions;
                    toExplode = int.Parse(mod[2..]);

                    if (explodeLower)
                    {
                        context.highlightMax = toExplode - 1;
                        context.highlightMin = 0;
                    }
                    else
                    {
                        context.highlightMax = context.d;
                        context.highlightMin = toExplode + 1;
                    }

                    int originalCount = r.Count;
                    int i = 0;
                    int rerolls = 0;
                    while (i < originalCount && rerolls < max)
                    {
                        if (r[i] != toExplode && (r[i] > toExplode ^ explodeLower))
                        {
                            r.Add(context.rand.Next(1, context.d + 1));
                            rerolls++;
                        }
                        i++;
                    }
                    return r;
                }
            }
        };
    }
}
