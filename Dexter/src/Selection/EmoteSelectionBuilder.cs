using System;
using System.Collections.Generic;
using System.Linq;
using Discord;

namespace Fergun.Interactive.Selection
{
    /// <summary>
    /// Represents a variant of <see cref="SelectionBuilder{TValue}"/> that allows using emotes as input.
    /// It provides overriden properties with default values that makes it ready to use with options using reactions or buttons as input.
    /// </summary>
    /// <typeparam name="TValue">The type of the value that represents a specific emote.</typeparam>
    public sealed class EmoteSelectionBuilder<TValue>
        : BaseSelectionBuilder<Selection<KeyValuePair<IEmote, TValue>>, KeyValuePair<IEmote, TValue>, EmoteSelectionBuilder<TValue>>
    {
        /// <inheritdoc/>
        public override Func<KeyValuePair<IEmote, TValue>, IEmote>? EmoteConverter { get; set; } = pair => pair.Key;

        /// <inheritdoc/>
        public override IEqualityComparer<KeyValuePair<IEmote, TValue>> EqualityComparer { get; set; } = new EmoteComparer<TValue>();

        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        public new IDictionary<IEmote, TValue> Options { get; set; } = new Dictionary<IEmote, TValue>();

        /// <summary>
        /// Initializes a new instance of the <see cref="EmoteSelectionBuilder{TValue}"/> class.
        /// </summary>
        public EmoteSelectionBuilder()
        {
        }

        /// <inheritdoc/>
        public override Selection<KeyValuePair<IEmote, TValue>> Build()
            => new(EmoteConverter, StringConverter,
                EqualityComparer, AllowCancel, SelectionPage.Build(), Users.ToArray(), Options.ToArray(),
                CanceledPage?.Build(), TimeoutPage?.Build(), SuccessPage?.Build(), Deletion, InputType,
                ActionOnCancellation, ActionOnTimeout, ActionOnSuccess);

        /// <summary>
        /// Sets the options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns>This builder.</returns>
        public EmoteSelectionBuilder<TValue> WithOptions(IDictionary<IEmote, TValue> options)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            return this;
        }

        /// <summary>
        /// Adds an option.
        /// </summary>
        /// <param name="emote">The emote.</param>
        /// <param name="value">The value.</param>
        /// <returns>This builder.</returns>
        public EmoteSelectionBuilder<TValue> AddOption(IEmote emote, TValue value)
        {
            Options.Add(emote, value);
            return this;
        }
    }

    /// <summary>
    /// Represents a variant of <see cref="SelectionBuilder{TValue}"/> that allows using emotes as input.
    /// It provides overriden properties with default values that makes it ready to use with options using reactions or buttons as input.
    /// </summary>
    public sealed class EmoteSelectionBuilder : BaseSelectionBuilder<Selection<IEmote>, IEmote, EmoteSelectionBuilder>
    {
        /// <inheritdoc/>
        public override Func<IEmote, IEmote>? EmoteConverter { get; set; } = emote => emote;

        /// <inheritdoc/>
        public override IEqualityComparer<IEmote> EqualityComparer { get; set; } = new EmoteComparer();

        /// <summary>
        /// Initializes a new instance of the <see cref="EmoteSelectionBuilder"/> class.
        /// </summary>
        public EmoteSelectionBuilder()
        {
        }

        /// <inheritdoc/>
        public override Selection<IEmote> Build()
            => new(EmoteConverter, StringConverter,
                EqualityComparer, AllowCancel, SelectionPage?.Build()!, Users.ToArray(), Options.ToArray(),
                CanceledPage?.Build(), TimeoutPage?.Build(), SuccessPage?.Build(), Deletion, InputType,
                ActionOnCancellation, ActionOnTimeout, ActionOnSuccess);
    }

    internal class EmoteComparer<TValue> : IEqualityComparer<KeyValuePair<IEmote, TValue>>
    {
        public bool Equals(KeyValuePair<IEmote, TValue> x, KeyValuePair<IEmote, TValue> y)
        {
            return x.Key.ToString() == y.Key.ToString() && Equals(x.Value, y.Value);
        }

        public int GetHashCode(KeyValuePair<IEmote, TValue> pair)
        {
            return pair.GetHashCode();
        }
    }

    internal class EmoteComparer : IEqualityComparer<IEmote>
    {
        public bool Equals(IEmote x, IEmote y) => x?.ToString() == y?.ToString();

        public int GetHashCode(IEmote obj) => obj.ToString()?.GetHashCode() ?? 0;
    }
}