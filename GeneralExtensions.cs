namespace Constellation.Sitecore.Items.Tds
{
	using System;
	using System.Linq;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;

	/// <summary>
	/// Extensions used during T4 Constellation.Sitecore.Items item generation.
	/// </summary>
	/// <remarks>
	/// This extension library was forked from Hedgehog Development's T4 starter kit. 
	/// https://github.com/HedgehogDevelopment/tds-codegen.git
	/// I changed the namespace to ensure it doesn't conflict with other TDS projects
	/// you may have running.; I have made some slight modifications to produce class 
	/// names that I find a bit more pleasing.
	/// </remarks>
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "Reviewed. Suppression is OK here.")]
	public static class GeneralExtensions
	{
		/// <summary>
		/// Returns all elements in the collection except the last 'n'.
		/// </summary>
		/// <typeparam name="T">The type parameter.</typeparam>
		/// <param name="source">The source.</param>
		/// <param name="n">The number of elements to skip.</param>
		/// <example> 
		/// This sample shows how to call the method.
		/// <code>
		///     int[] ary = new int[5]{ 1, 2, 3, 4, 5};
		///     IEnumerable&lt;int&gt; results = ary.SkipLast(2);
		/// </code>
		/// The results variable would contain elements 1, 2, and 3.
		/// </example>
		/// <returns>All <typeparamref name="T"/> elements in the source collection except the last 'n'.</returns>
		public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int n)
		{
			var it = source.GetEnumerator();
			bool hasRemainingItems;
			var cache = new Queue<T>(n + 1);

			do
			{
				// ReSharper disable CSharpWarnings::CS0665
				if (hasRemainingItems = it.MoveNext())
				// ReSharper restore CSharpWarnings::CS0665
				{
					cache.Enqueue(it.Current);
					if (cache.Count > n)
					{
						yield return cache.Dequeue();
					}
				}
			}
			while (hasRemainingItems);
		}

		/// <summary>
		///     Recursively projects each nested element to an <see cref="IEnumerable{TSource}"/>
		///     and flattens the resulting sequences into one sequence.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">A sequence of values to project.</param>
		/// <param name="recursiveSelector">A transform to apply to each element.</param>
		/// <returns>
		///     An <see cref="IEnumerable{TSource}"/> whose elements are the
		///     result of recursively invoking the recursive transform function
		///     on each element and nested element of the input sequence.
		/// </returns>
		public static IEnumerable<TSource> SelectRecursive<TSource>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TSource>> recursiveSelector)
		{
			// ReSharper disable SuggestUseVarKeywordEvident
			Stack<IEnumerator<TSource>> stack = new Stack<IEnumerator<TSource>>();
			// ReSharper restore SuggestUseVarKeywordEvident
			stack.Push(source.GetEnumerator());

			try
			{
				while (stack.Count > 0)
				{
					if (stack.Peek().MoveNext())
					{
						TSource current = stack.Peek().Current;

						yield return current;

						stack.Push(recursiveSelector(current).GetEnumerator());
					}
					else
					{
						stack.Pop().Dispose();
					}
				}
			}
			finally
			{
				while (stack.Count > 0)
				{
					stack.Pop().Dispose();
				}
			}
		}

		/// <summary>
		/// Gets the value of the item in the dictionary at the specified key.
		/// if the key is not found, returns an empty string.
		/// </summary>
		/// <param name="dictionary">The dictionary.</param>
		/// <param name="key">The key.</param>
		/// <remarks>This method is very useful within a T4 file to pull out a specific value of the <c>SitecoreFields</c> property.</remarks>
		/// <returns>The value at the specifed entry, or "" if the key does not exist.</returns>
		public static string GetValue(this Dictionary<string, string> dictionary, string key)
		{
			string @out;
			if (!dictionary.TryGetValue(key, out @out))
			{
				@out = string.Empty;
			}

			return @out;
		}

		/// <summary>
		/// Returns distict items based on the key selector function.
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <typeparam name="TKey">The type of the key</typeparam>
		/// <param name="source">The sequence of values to remove duplicate items from.</param>
		/// <param name="keySelector">A function that returns a value for the key based on the input item.</param>
		/// <returns>A sequence of unique elements based on the key.</returns>
		public static IEnumerable<TSource> Distinct<TSource,TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.Distinct(new EqualityComparer<TSource, TKey>(keySelector));
		}

		private class EqualityComparer<TSource, TKey> : IEqualityComparer<TSource>
		{
			private Func<TSource, TKey> keySelector;

			public EqualityComparer(Func<TSource, TKey> keySelector)
			{
				this.keySelector = keySelector;
			}


			public bool Equals(TSource x, TSource y)
			{
				return object.Equals(keySelector(x), keySelector(y));
			}

			public int GetHashCode(TSource obj)
			{
				return keySelector(obj).GetHashCode();
			}
		}
	}
}
