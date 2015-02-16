// =================================================================================
// Copyright (c) 2014 Bndy.Net
// Created by Bndy at 2014-10-25 8:43:01
// ---------------------------------------------------------------------------------
// Summary & Change Logs.
// =================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace TaskUI.Lib
{
	public abstract partial class Project
	{
		/// <summary>
		/// Converts the object to specified type.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="o">The source object.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns>An instance typed T.</returns>
		protected T ConvertType<T>(object o, T defaultValue)
		{
			try
			{
				if (typeof(T).IsClass)
				{
					o = System.Convert.ChangeType(o, typeof(T));
				}
				return (T)o;
			}
			catch
			{
				return defaultValue;
			}
		}

		/// <summary>
		/// Updates the properties from one model to another model.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="source">The source.</param>
		/// <param name="destination">The destination.</param>
		protected void UpdateProperties<T>(T source, ref T destination) where T : new()
		{
			if (destination == null)
				destination = (T)Activator.CreateInstance(typeof(T));

			foreach (var p in typeof(T).GetProperties())
			{
				if (p.Name.StartsWith("_")) continue;
				p.SetValue(destination, p.GetValue(source));
			}
		}

		/// <summary>
		/// Converts the row to model.
		/// </summary>
		/// <typeparam name="TModel">The type of the model. The properties of model MUST be same names as database fields.</typeparam>
		/// <param name="row">The row.</param>
		/// <returns>TModel.</returns>
		protected TModel ConvertRowToModel<TModel>(Dictionary<string, object> row)
			where TModel : new()
		{
			if (row == null) return default(TModel);

			var list = new List<Dictionary<string, object>>();
			list.Add(row);
			return ConvertRowsToModels<TModel>(list).FirstOrDefault();
		}

		/// <summary>
		/// Converts the rows to models automatically.
		/// </summary>
		/// <typeparam name="TModel">The type of the model. The properties of model MUST be same names as database fields.</typeparam>
		/// <param name="rows">The rows.</param>
		/// <returns>List{``0}.</returns>
		protected List<TModel> ConvertRowsToModels<TModel>(List<Dictionary<string, object>> rows)
			where TModel : new()
		{
			List<TModel> result = new List<TModel>();

			if (rows != null)
			{
				foreach (var d in rows)
				{
					TModel m = (TModel)Activator.CreateInstance(typeof(TModel));
					foreach (var p in m.GetType().GetProperties())
					{
						if (d.ContainsKey(p.Name))
							p.SetValue(m, d[p.Name]);
					}
					result.Add(m);
				}
			}

			return result;
		}

		/// <summary>
		/// Compares the text between two html strings.
		/// </summary>
		/// <param name="html1">The HTML1.</param>
		/// <param name="html2">The HTML2.</param>
		/// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
		protected bool IsSameText(string html1, string html2)
		{
			var result = false;

			html1 = GetTextInHtml(html1);
			html2 = GetTextInHtml(html2);

			var replacements = new Dictionary<string, string>() { 
				{@"\s+", ""},
			};
			html1 = RegexReplace(html1, replacements);
			html2 = RegexReplace(html2, replacements);


			if (html1 == html2)
				result = true;

			return result;
		}

		/// <summary>
		/// Gets the next list order.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="field">The field.</param>
		/// <param name="currentOrder">The current order.</param>
		/// <param name="increase">The increase.</param>
		/// <returns>System.Decimal.</returns>
		protected decimal GetNextListOrder(string table, string field, decimal currentOrder,
			int increase = 10)
		{
			var next = GetTopNRows(1, table, string.Format("{0} > {1}", field, currentOrder),
				string.Format("{0} ASC", field)).FirstOrDefault();

			if (next != null)
			{
				if (next[field] != null)
				{
					var e = decimal.Parse(next[field].ToString());

					return (e + currentOrder) / 2;
				}
			}

			return currentOrder + increase;
		}
		/// <summary>
		/// Gets the progress value percent.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="totals"></param>
		/// <returns></returns>
		protected double GetProgressValue(int[] values, int[] totals)
		{
			double result = 0d;
			double[] seeds = new double[values.Length];
			for (var i = 0; i < totals.Length; i++)
			{
				if (i > 0)
					seeds[i] = seeds[i - 1] / totals[i];
				else
					seeds[i] = 100d / totals[i];

				result += (values[i]) * seeds[i];
			}

			return result;
		}

		/// <summary>
		/// Cleans up and fix HTML.
		///		Drop the useless string, like comments, scripts, fonts and so on.
		///		And close the html tags.
		/// </summary>
		/// <param name="html">The HTML.</param>
		/// <returns>System.String.</returns>
		protected string CleanUpAndFixHtml(string html)
		{
			if (!string.IsNullOrWhiteSpace(html))
			{
				html = DropUselessString(html);

				// close the html tags using 3rd party dll.
				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(html);
				html = doc.DocumentNode.OuterHtml;
			}

			return html;
		}
		/// <summary>
		/// Drops the useless string.
		///		Html Comments, Scripts and Fonts
		/// </summary>
		/// <param name="html">The HTML.</param>
		/// <returns>System.String.</returns>
		private string DropUselessString(string html)
		{
            string result = html;

			if (!string.IsNullOrWhiteSpace(html))
			{
				result = Regex.Replace(result, @"<!--[\s\S]*?-->", "");
				result = Regex.Replace(result, @"</?font.*?>", "", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"font-family\s*:\s*.*?(;|(?=[""']))", "", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"font-size\s*:\s*[\w\d\.]*?(;|(?=[""']))", "", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @" class\s*=\s*(['""].*?['""]|\w+)", "", RegexOptions.IgnoreCase);
				result = Regex.Replace(result, @"<script[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
			}

			return result.Trim();
		}

		/// <summary>
		/// Gets the text in HTML.
		/// </summary>
		/// <param name="html">The HTML.</param>
		/// <returns>System.String.</returns>
		protected string GetTextInHtml(string html)
		{
			if (!string.IsNullOrEmpty(html))
			{
				return RegexReplace(html, new Dictionary<string, string>() { 
					{@"<.*?>|&\w+;", ""},
					{@"\s{2,}", " "}
				}).Trim();
			}

			return html;
		}
	}
}
