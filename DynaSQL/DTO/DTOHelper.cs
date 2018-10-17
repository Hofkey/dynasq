using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace dynasq.DTO.Helper
{
    public class DTOHelper
    {
        /// <summary>
        /// Dit zorgt ervoor dat een database model wordt vertaald naar een DTO.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>DTO</returns>
        public object ConvertModel(object from, object to)
        {
            Type fType = from.GetType();
            Type tType = to.GetType();

            PropertyInfo[] fmpi = fType.GetProperties();
            PropertyInfo[] tmpi = tType.GetProperties();

            foreach(var pi in  tmpi)
            {
                if (pi.CanWrite)
                {
                    var fpi = fmpi.SingleOrDefault(item => item.Name.ToLower() == pi.Name.ToLower());

                    if (fpi != null)
                    {
                        if (pi.PropertyType.IsPrimitive || pi.PropertyType == typeof(string))
                        {
                            pi.SetValue(to, fpi.GetValue(from, null));
                        }
                        else if (pi.PropertyType.IsClass)
                        {
                            var toConvert = Assembly.GetExecutingAssembly().CreateInstance(from.GetType().Namespace + '.' + fpi.Name);
                            var final = Assembly.GetExecutingAssembly().CreateInstance(to.GetType().Namespace + '.' + pi.PropertyType.Name);
                            toConvert = FilterFromChildClass(from, fpi.Name).GetValue(from, null);
                            PropertyInfo[] oInfo = toConvert.GetType().GetProperties();
                            PropertyInfo[] pInfo = pi.PropertyType.GetProperties();

                            foreach (var spi in pInfo)
                            {
                                if (spi.CanWrite)
                                {
                                    var match = oInfo.SingleOrDefault(item => item.Name.ToLower() == spi.Name.ToLower());
                                    if (match != null)
                                    {
                                        var val = match.GetValue(toConvert, null);
                                        spi.SetValue(final, val, null);
                                    }
                                }
                            }

                            pi.SetValue(to, final);
                        }
                    }
                }
            }

            try
            {
                var relConverted = ConvertRelations(from, to);
            }
            catch (Exception)
            {
                return to;
            }

            return to;
        }

        /// <summary>
        /// This function converts relation properties to the corresponding "to" properties
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="to"></param>
        /// <returns>Converted object</returns>
        private object ConvertRelations(object origin, object to)
        {
            List<PropertyInfo> relationProperties = new List<PropertyInfo>();
            PropertyInfo[] cRels = to.GetType().GetProperties();

            foreach (var property in origin.GetType().GetProperties())
            {
                if (property.Name.EndsWith("Relatie") || property.Name.EndsWith("Rel") || property.Name.EndsWith("Relaties"))
                {
                    if(property.PropertyType.IsClass)
                        relationProperties.Add(property);
                }
            }

            if(relationProperties.Count == 0)
            {
                return null;
            }

            foreach (var relProp in relationProperties)
            {
                var parent = relProp.GetValue(origin, null);
                var parentProps = parent.GetType().GetProperties();
                object match;
                foreach(var rel in cRels)
                {
                    if (rel.PropertyType.IsClass)
                    {
                        var link = parentProps.SingleOrDefault(pr => pr.Name == rel.Name);
                        if (link != null)
                        {
                            match = link.GetValue(parent, null);
                            rel.SetValue(to, ConvertModel(match, Assembly.GetExecutingAssembly().CreateInstance("TicketSystemDb.DTO."+rel.PropertyType.Name)), null);
                        }
                    }
                }
            }
            return to;
        }

        /// <summary>
        /// This gets the correct child class if there are multiple properties with the same name.
        /// </summary>
        /// <param name="_base"></param>
        /// <param name="property"></param>
        /// <returns>The child class</returns>
        private PropertyInfo FilterFromChildClass(object _base, string property)
        {
            PropertyInfo[] baseInfo = _base.GetType().GetProperties();
            var res = baseInfo.Where(prop => prop.Name.Contains(property));

            foreach(var prop in res)
            {
                if(prop.PropertyType.IsClass)
                {
                    if (!prop.PropertyType.IsPrimitive || prop.PropertyType != typeof(string))
                    {
                        return prop; ;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Uppercases first letter of string.
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Formatted string.</returns>
        private string UppercaseFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            return char.ToUpper(s[0]) + s.Substring(1);
        }

        /// <summary>
        /// Checks which properties need to be skipped.
        /// NOTE: there's still no practical use for this.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns>Boolean</returns>
        private bool ToSkip(PropertyInfo pi)
        {
            foreach(var att in pi.GetCustomAttributes())
            {
                //if(att.)
            }
            return false;
        }
    }
}
