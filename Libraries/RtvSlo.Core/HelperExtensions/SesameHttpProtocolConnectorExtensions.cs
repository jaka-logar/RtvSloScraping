using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Storage;
using RtvSlo.Core.Helpers;
using VDS.RDF.Query;
using Castle.Core.Logging;
using RtvSlo.Core.Infrastructure.Windsor;

namespace RtvSlo.Core.HelperExtensions
{
    public static class SesameHttpProtocolConnectorExtensions
    {
        private static ILogger logger = DependencyContainer.Instance.Resolve<ILogger>();

        public static SparqlResultSet QueryFormat(this SesameHttpProtocolConnector connector, string format, params object[] args)
        {
            string innerQuery = string.Format(format, args);
            return SesameHttpProtocolConnectorExtensions.QueryFormat(connector, innerQuery);

            //try
            //{
            //    string innerQuery = string.Format(format, args);
            //    string query = string.Format("{0} {1}", RepositoryHelper.Prefixes, innerQuery);
            //    SparqlResultSet result = connector.Query(query) as SparqlResultSet;

            //    if (result == null)
            //    {
            //        logger.FatalFormat("SesameHttpProtocolConnectorExtensions, QueryFormat, result is null - QUERY: {0}", innerQuery);
            //    }

            //    if (result.Results.IsEmpty())
            //    {
            //        logger.WarnFormat("SesameHttpProtocolConnectorExtensions, QueryFormat, result is empty - QUERY: {0}", innerQuery);
            //    }

            //    return result;
            //}
            //catch (Exception ex)
            //{
            //    logger.FatalFormat("SesameHttpProtocolConnectorExtensions, QueryFormat - EXCEPTION: {0}", ex.Message);

            //    return null;
            //}
        }

        public static SparqlResultSet QueryFormat(this SesameHttpProtocolConnector connector, string innerQuery)
        {
            try
            {
                string query = string.Format("{0} {1}", RepositoryHelper.Prefixes, innerQuery);
                SparqlResultSet result = connector.Query(query) as SparqlResultSet;

                if (result == null)
                {
                    logger.FatalFormat("SesameHttpProtocolConnectorExtensions, QueryFormat, result is null - QUERY: {0}", innerQuery);
                }

                if (result.Results.IsEmpty())
                {
                    logger.WarnFormat("SesameHttpProtocolConnectorExtensions, QueryFormat, result is empty - QUERY: {0}", innerQuery);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.FatalFormat("SesameHttpProtocolConnectorExtensions, QueryFormat - EXCEPTION: {0}", ex.Message);

                return null;
            }
        }
    }
}
