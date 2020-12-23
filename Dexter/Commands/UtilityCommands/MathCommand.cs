using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dexter.Attributes.Methods;
using Dexter.Enums;
using Dexter.Extensions;
using Discord.Commands;

namespace Dexter.Commands {
	public partial class UtilityCommands {

		private const int MAX_ROLLS = 999999; //Should probably be moved to a config setting
		
		/// <summary>
		/// Evaluates a mathematical expression and gives a result or throws an error.
		/// </summary>
		
		[Command("math")]
		[Summary("Evaluates a mathematical expression")]
		[ExtendedSummary(
			"Evaluates a given mathematical expression. \n" +
			"Basic arithmetic operators are: + - / * ^. Remainder: %, Factorial: ! \n" +
			"The random operator, \'d\', allows you to roll dice! 1d20 rolls a twenty-sided die, 4d6 adds up the rolls of four six-sided dice. \n" +
			"You can also use functions such as sqrt(a), floor(a), abs(a), ln(a), log(b, a), max(a, b, c...), etc. \n" +
			"A few mathematical and physical constants are available. \'pi\', \'e\', \'phi\', \'c\', \'electron\', etc."
		)]
		[Alias("calc", "calculate")]

        public async Task MathCommand([Remainder] string expression) {
			MathResult r = new MathResult(expression);

			if (!r.errorFlag) {
				await BuildEmbed(EmojiEnum.Love)
					.WithTitle($"Evaluating: **{expression}**.")
					.WithDescription(r.result.ToString())
					.AddField(r.rolls != "", "Rolls:", r.rolls)
					.SendEmbed(Context.Channel);
			} else {
				await BuildEmbed(EmojiEnum.Annoyed)
					.WithTitle($"ERROR! Received: `{expression}`.")
					.WithDescription($"{r.error}\n{r}")
					.SendEmbed(Context.Channel);
			}
		}

		private static readonly Dictionary<string, double> constants = new Dictionary<string, double>() {
			{"epsilon", 8.8541878128e-12},
			{"electron", 1.60217662e-19},
			{"phi", 1.618033988749},
			{"G", 6.67408e-11},
			{"pi", Math.PI},
			{"mu0", 1.2566370621219e-6},
			{"k", 8.9875517923e9},
			{"e", Math.E},
			{"c", 299792458.0}
		};

		private class Function {
			public Func<double, double> f;
			public Func<double, bool> domain;

			public Function(Func<double, double> f, Func<double, bool> domain) {
				this.f = f;
				this.domain = domain;
			}
		}

		private static readonly Dictionary<string, Function> functions = new Dictionary<string, Function>() {
			{"abs", new Function(Math.Abs, d => true)},
			{"sqrt", new Function(Math.Sqrt, IsPositive)},
			{"cbrt", new Function(Math.Cbrt, d => true)},
			{"ln", new Function(Math.Log, IsStrictPositive)},
			{"tan", new Function(Math.Tan, d => true)},
			{"sin", new Function(Math.Sin, d => true)},
			{"cos", new Function(Math.Cos, d => true)},
			{"arctan", new Function(Math.Atan, d => true)},
			{"arcsin", new Function(Math.Asin, IsLessThanOne)},
			{"arccos", new Function(Math.Acos, IsLessThanOne)},
			{"ceil", new Function(Math.Ceiling, d => true)},
			{"floor", new Function(Math.Floor, d => true)}
		};

		private static bool IsLessThanOne(double d) { return Math.Abs(d) <= 1; }
		private static bool IsStrictPositive(double d) { return d > 0; }
		private static bool IsPositive(double d) { return d >= 0; }

		private class MVFunction {
			public Func<double[], double> f;
			public Func<double[], bool> domain;

			public MVFunction(Func<double[], double> f, Func<double[], bool> domain) {
				this.f = f;
				this.domain = domain;
			}
		}

		private static readonly Dictionary<string, MVFunction> mvfunctions = new Dictionary<string, MVFunction>() {
			{"max", new MVFunction(GetMax, d => true)},
			{"min", new MVFunction(GetMin, d => true)},
			{"log", new MVFunction(MVLog, MVLogCheck)}
		};

		private static double GetMax(double[] d) { return d.Max(); }
		private static double GetMin(double[] d) { return d.Min(); }
		private static double MVLog(double[] d) { return Math.Log(d[1]) / Math.Log(d[0]); }

		private static bool MVLogCheck(double[] d) { return d.Length == 2 && d[0] > 0 && d[1] > 0; }

		private static double ProcessMath(string arg, MathResult result, double neutralValue = 1) {
			//Console.WriteLine(arg);
			if (result.errorFlag)
				return 1;

			if (arg == "")
				return neutralValue;

			//parsing parentheses
			int start = 0;
			int depth = 0;
			while (arg.Contains('(') || arg.Contains(')')) {
				for (int i = 0; i < arg.Length; i++) {
					if (arg[i] == '(' && depth++ == 0)
						start = i;
					else if (arg[i] == ')') {
						if (--depth == 0) {
							//Simplify everything within the parentheses
							arg = Simplify(arg, arg[(start + 1)..i], start, i, result);
							//Console.WriteLine("New simplified arg = " + arg);
							break;
						} else if (depth < 0) {
							return result.ThrowError("Unbalanced or unexpected closing parenthesis found.");
						}
					}
				}
			}
			if (depth > 0)
				return result.ThrowError("Unbalanced or unexpected opening parenthesis found.");

			double a;
			double b;

			//parsing addition/subtraction
			for (int i = arg.Length - 1; i >= 0; i--) {
				if (arg[i] == '+' || arg[i] == '-') {
					if (i > 0 && arg[i - 1] == 'E')
						continue; //if the + or - is part of an order of magnitude expression, pass.
					a = ProcessMath(arg[0..i], result, 0);
					b = ProcessMath(arg[(i + 1)..], result, 0);

					if (arg[i] == '+') {
						result.Echo("Added " + a + " + " + b + ".", arg);
						return a + b;
					} else {
						result.Echo("Subtracted " + a + " - " + b + ".", arg);
						return a - b;
					}
				}
			}

			//parsing multiplication/division
			for (int i = arg.Length - 1; i >= 0; i--) {
				if (arg[i] == '*' || arg[i] == '/') {
					a = ProcessMath(arg[0..i], result, 1);
					b = ProcessMath(arg[(i + 1)..], result, 1);

					if (b == 0)
						return result.ThrowError("Attempt to divide by zero");
					if (arg[i] == '*') {
						result.Echo("Multiplied " + a + " * " + b, arg);
						return a * b;
					} else {
						result.Echo("Divided " + a + " / " + b, arg);
						return a / b;
					}
				}
			}

			//parsing powers
			for (int i = arg.Length - 1; i >= 0; i--) {
				if (arg[i] == '^') {
					a = ProcessMath(arg[0..i], result, 1);
					b = ProcessMath(arg[(i + 1)..], result, 1);

					if (a == 0 && b == 0)
						return result.ThrowError("Attempt to evaluate zero to the power of zero.");

					result.Echo("Evaluated " + a + " ^ " + b, arg);
					return Math.Pow(a, b);
				}
			}

			//parsing the random operator and the remainder operator
			for (int i = arg.Length - 1; i >= 0; i--) {
				if (arg[i] == 'd' || arg[i] == '%') {
					a = ProcessMath(arg[0..i], result, 1);
					b = ProcessMath(arg[(i + 1)..], result, 1);

					if (arg[i] == 'd') { return Roll(a, b, result); }
					return a % b;
				}
			}

			//parsing factorials
			if (arg[^1] == '!') {
				return Factorial(ProcessMath(arg[0..^1], result), result);
			}

			string funcComp = arg;
			string funcNum = "";
			double factor = 1;
			for (int i = 0; i < arg.Length; i++) {
				if (!Char.IsDigit(arg[i])) {
					funcNum = arg[0..i];
					funcComp = arg[i..].ToLower();
					break;
				}
			}
			if (funcNum.Length > 0 && funcComp.Length > 0) {
				result.Echo($"Parsing function multiplicand \"{funcNum}\"", arg);
				factor = ProcessMath(funcNum, result, 1);
			}

			//parsing multivar functions
			foreach (string f in mvfunctions.Keys) {
				if (funcComp.StartsWith(f)) {
					arg = arg[f.Length..];
					double[] arr = new double[1];
					if (arg.Length < 3 || arg[0] != '[' || arg[^1] != ']') { //multiparameters have the format [a;b;c; ... ;x;y;z]
						if (functions.ContainsKey(f))
							break;
						if (arg.Length > 0 && arg[0] != '[') {
							arr[0] = ProcessMath(arg, result, 1);
							if (!mvfunctions[f].domain(arr))
								return result.ThrowError($"Single argument for multiparameter function {f} is invalid, found \"{arg}\"");
							result.Echo($"Evaluating multivar function {f} with a single parameter {arr[0]}.", arg);
							return factor * mvfunctions[f].f(arr);
						}
						return result.ThrowError($"Invalid arguments for multiparametric function {f}. Found \"{arg}\".");
					}
					List<double> parameters = new List<double>();
					depth = 0;
					start = 1;
					for (int i = 1; i < arg.Length; i++) {
						if (arg[i] == '[')
							depth++;
						if (arg[i] == ']' && depth-- < 0)
							return result.ThrowError($"Unbalanced or unexpected closing brackets in multiparametric function evaluation for function {f}. Found \"{arg}\"");
						if ((arg[i] == ';' && depth == 0) || (arg[i] == ']' && depth == -1)) {
							parameters.Add(ProcessMath(arg[start..i], result, 1));
							Console.WriteLine("Added new parameter " + parameters[^1]);
							start = i + 1;
						}
					}
					if (depth > 0)
						return result.ThrowError($"Unbalanced or unexpected opening brackets in multiparametric function evaluation for function {f}. Found \"{arg}\"");
					arr = parameters.ToArray();
					if (!mvfunctions[f].domain(arr))
						return result.ThrowError($"Invalid argument set for function {f}. Found [{string.Join("; ", arr)}].");
					result.Echo($"Evaluating multivar function {f} with parameter array [{string.Join("; ", arr)}].", arg);
					return factor * mvfunctions[f].f(arr);
				}
			}

			//parsing functions
			foreach (string f in functions.Keys) {
				if (arg.ToLower().StartsWith(f)) {
					a = ProcessMath(arg[f.Length..], result, 1);
					if (!functions[f].domain(a))
						return result.ThrowError($"Value {a} not included in the domain of function \"{f}\"");
					result.Echo($"Evaluating {f} of {a}.", arg);
					return functions[f].f(a);
				}
			}

			//parsing numbers
			foreach (string k in constants.Keys) {
				if (arg.Length - k.Length < 0)
					continue;
				if (arg[^k.Length..] == k) {
					result.Echo("Parsed constant " + k + " = " + constants[k] + ".", arg);
					return ProcessMath(arg[0..^k.Length], result, 1) * constants[k];
				}
			}

			int sign = 1;
			if (arg[0] == '_') { arg = arg[1..]; sign = -1; }
			if (Double.TryParse(arg, out double d)) {
				result.Echo("Parsed numerical value " + d * sign + ".", (sign == -1 ? "(-)" : "") + arg);
				return d * sign;
			} else {
				return result.ThrowError("Failed to parse string \"" + arg + "\".");
			}
		}

		internal class MathResult {
			public bool errorFlag = false;
			public double result = 0;
			public string error = "";
			public List<string> verbose = new List<string>();
			public List<string> verboseStack = new List<string>();
			public string rolls = "";

			public MathResult(string arg) {
				this.result = ProcessMath(arg.Replace(" ", ""), this);
			}

			public double ThrowError(string error) {
				this.error += '\n' + error;
				errorFlag = true;
				return 1;
			}

			public void Echo(string message, string stack) {
				this.verbose.Add(message);
				this.verboseStack.Add(stack);
			}

			public override string ToString() {
				string s = "";
				for (int i = 0; i < verbose.Count && i < verboseStack.Count; i++) {
					s += $" v: {verbose[i]} with arg = {verboseStack[i]}\n";
				}
				return s;
			}

			public void NewDice(int d) {
				rolls += $"d{d}:";
            }

			public void NewRoll(int r) {
				rolls += $" {r},";
            }

			public void EndDice() {
				rolls = rolls[..^1] + '\n';
            }
		}

		private static string Simplify(string str, string eval, int start, int end, MathResult result) {
			string left = str[0..start];
			string right = str[(end + 1)..];

			if (eval.Contains(",")) { //if multiparameter syntax is found, it's converted into "[a; b; c; d;...]" for later processing
				result.Echo("Parsing function parameters \"" + eval + "\"", str);

				int depth = 0;
				for (int i = 1; i < eval.Length; i++) {
					if (eval[i] == '(')
						depth++;
					if (eval[i] == ')')
						depth--;
					else if (eval[i] == ',' && depth == 0)
						eval = eval[0..i] + ';' + eval[(i + 1)..];
				}

				//result.Echo("Parsed function parameters to \"" + eval + "\"", str);
				return left + "[" + eval + "]" + right;
			}

			//otherwise, multiplication shorthands are considered
			bool asteriskLeft = start > 0 && Char.IsDigit(str[start - 1]);
			bool asteriskRight = end + 1 < str.Length && Char.IsDigit(str[end + 1]);

			double val = ProcessMath(eval, result, 1);
			result.Echo("Parsing parentheses (" + eval + ") into (" + val + ")", str);
			return $"{left}{(asteriskLeft ? "*" : "")}{(val < 0 ? "_" : " ")}{Math.Abs(val)}{(asteriskRight ? "*" : "")}{right}";
		}

		private static double Roll(double a, double b, MathResult result) {
			if (a > MAX_ROLLS)
				return result.ThrowError($"Exceeded maximum allowed random operations ({a} > {MAX_ROLLS})");
			
			int n = (int)Math.Round(a);

			if (n == 0) {
				result.Echo("Rolled 0 dice.", "a" + "b");
				return 0;
			}

			int d = (int)Math.Round(b);

			if (d < 1)
				return result.ThrowError("Attempt to roll a die with less than one face.");

			result.NewDice(d);

			Random rnd = new Random();
			double count = 0;
			int sign = 1;
			if (n < 0) { sign = -1; n = -n; }

			string verb = "Rolled values: ";

			for (int i = 0; i < n; i++) {
				int newValue = rnd.Next(1, d + 1) * sign;
				verb += newValue + ", ";
				count += newValue;
				result.NewRoll(newValue);
			}

			result.EndDice();
			result.Echo($"{verb[0..^2]} on {(sign == -1 ? "-" : "")}{n}d{d}.", a + "d" + b);
			return count;
		}

		private static double Factorial(double operand, MathResult result) {
			double value = 1;
			int a = (int)Math.Round(operand);
			for (int i = a; i > 1; i--) {
				value *= i;
				if (double.IsInfinity(value)) {
					return result.ThrowError("Overflow in factorial operation, result of local expression is infinity.");
				}
			}

			result.Echo($"Calculated the factorial of {operand}, rounded to {a}!", operand + "!");
			return value;
		}
	}
}

