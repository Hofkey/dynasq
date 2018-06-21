using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DynaSQL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.CSharp;
using Newtonsoft.Json;
//dddccccccc
/// dddedddd
namespace DynaSQL
{
    public class Queryfy
    {
        private DbContext _context;

        public Queryfy(DbContext context)
        {
            this._context = context;
        }

        /// <summary>
        /// function to compare the common properties and their values.
        /// \n the results will be put in a list which is first filled with all the results from the corrosponding tables in the database.
        /// \n after this the models with the same values from the common properties will be filtered and returned.
        /// </summary>
        public Task<List<dynamic>> FilterSelectAsync(object model, List<QueriableItem> queriables)
        {
            return Task.Run(() => FilterSelect(model, queriables));
        }

        /// <summary>
        /// function to compare the common properties and their values.
        /// \n the results will be put in a list which is first filled with all the results from the corrosponding tables in the database.
        /// \n after this the models with the same values from the common properties will be filtered and returned.
        /// </summary>
        public async Task<List<dynamic>> FilterSelect(object model, List<QueriableItem> queriables)
        {
            // get the type of the model.
            Type t = model.GetType();
            // get the properties from the model to compare them.
            PropertyInfo[] mpi = t.GetProperties();
            // get all the results from the corrosponding table in the database.
            List<dynamic> results = await Task.FromResult(GetMembers(_context, t.Name).OfType<dynamic>().ToList());

            // start looping through the properties.
            foreach (PropertyInfo pi in mpi)
            {
                // check if the property maches any property from the other model.
                var match = queriables.SingleOrDefault(m => m.PropName == pi.Name);
                if (match != null)
                {
                    // get the index of the match.
                    var index = queriables.IndexOf(match);
                    // filter the result list.
                    results = results.Where(m => pi.GetValue(m, null) == queriables[index].PropValue).ToList();
                }
            }

            // finally return the filtered results.
            return results;
        }

        // function to get members from a dbset.
        private IEnumerable GetMembers(DbContext db, string setName)
        {
            // get the property.
            var res = db.GetType().GetProperty(setName);
            // return the set.
            return (IEnumerable)res.GetValue(db);
        }


        /// <summary>
        /// Converts non-db model to db model.
        /// </summary>
        /// <returns>The db model.</returns>
        /// <param name="parent">Parent.</param>
        /// <param name="toConvert">To convert.</param>
        public dynamic ConvertToDbModel(object parent, object toConvert)
        {
            Type pt = parent.GetType();
            Type ct = toConvert.GetType();
            PropertyInfo[] mpip = pt.GetProperties();
            PropertyInfo[] mpic = ct.GetProperties();

            foreach (PropertyInfo pi in mpic)
            {
                var corProp = mpip.SingleOrDefault(p => p.Name == pi.Name);
                if (corProp != null)
                {
                    corProp.SetValue(parent, pi.GetValue(toConvert, null));
                }
            }

            return parent;
        }

        /// <summary>
        /// Initializes the queriables.
        /// </summary>
        /// <returns>The queriables.</returns>
        /// <param name="model">Model.</param>
        /// <param name="filters">Filters.</param>
        public List<QueriableItem> InitializeQueriables(object model, List<string> filters = null)
        {
            List<QueriableItem> results = new List<QueriableItem>();
            // get the type of the given model.
            Type type = model.GetType();
            // get all the properties from the model.
            PropertyInfo[] mpi = type.GetProperties();

            // check if filters have been given.
            if (filters != null)
            {
                // loop through the properties.
                foreach (var pi in mpi)
                {
                    var match = filters.SingleOrDefault(f => f == pi.Name);
                    if (match != null)
                    {
                        if (pi.GetValue(model, null) != null)
                        {
                            // add the property as a queriable item to the list.
                            results.Add(new QueriableItem { PropName = pi.Name, PropValue = pi.GetValue(model, null) });
                        }
                    }
                }
            }
            else
            {
                // loop through the properties.
                foreach (var pi in mpi)
                {
                    // check if there's a mich with the given filters.
                    var match = filters.SingleOrDefault(f => f == pi.Name);
                    if (match != null)
                    {
                        // add the property as a queriable item to the list.
                        results.Add(new QueriableItem { PropName = pi.Name, PropValue = pi.GetValue(model, null) });
                    }
                }
            }

            // return the list.
            return results;
        }


        /// <summary>
        /// Gets the values as string from every property in the range of the index till the end.
        /// </summary>
        /// <returns>The values of the indexes after the given index.</returns>
        /// <param name="model">Model.</param>
        /// <param name="index">Index.</param>
        public List<string> GetValuesFromIndex(object model, int index)
        {
            List<string> values = new List<string>();

            Type type = model.GetType();
            PropertyInfo[] mpi = type.GetProperties();

            while (index < mpi.Length)
            {
                values.Add((string)mpi[index].GetValue(model, null));
                index++;
            }

            return values;
        }

        /// <summary>
        /// Gets the values of indexes.
        /// </summary>
        /// <returns>The values of indexes.</returns>
        /// <param name="model">Model.</param>
        /// <param name="indexes">Indexes.</param>
        public List<string> GetValuesOfIndexes(object model, int[] indexes)
        {
            List<string> values = new List<string>();

            Type type = model.GetType();
            PropertyInfo[] mpi = type.GetProperties();

            foreach (int index in indexes)
            {
                values.Add((string)mpi[index].GetValue(model, null));
            }

            return values;
        }

        private bool IsJSON(string suspect)
        {
            try
            {
                var decoded = JsonConvert.DeserializeObject(suspect);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
