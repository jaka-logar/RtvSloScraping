using System;
using System.Collections.Generic;
using System.Text;
using RtvSlo.Core.Configuration;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.RdfPredicate;
using VDS.RDF;

namespace RtvSlo.Core.Helpers
{
    public static class RepositoryHelper
    {
        public static string Prefixes
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("PREFIX   rdf:    <http://www.w3.org/1999/02/22-rdf-syntax-ns#> ");
                sb.AppendLine("PREFIX	rdfs:	<http://www.w3.org/2000/01/rdf-schema#> ");
                sb.AppendLine("PREFIX	owl:	<http://www.w3.org/2002/07/owl#> ");
                sb.AppendLine("PREFIX	xsd:	<http://www.w3.org/2001/XMLSchema#> ");
                sb.AppendLine("PREFIX	dct:	<http://purl.org/dc/terms/> ");
                sb.AppendLine("PREFIX	sioc:	<http://rdfs.org/sioc/ns#> ");
                sb.AppendLine("PREFIX	news:	<http://opendata.lavbic.net/news/> ");
                sb.Append(string.Format("PREFIX 	mmc:	<{0}> ", RepositoryHelper.BaseUrl));

                sb.AppendLine("PREFIX   geo:    <http://www.w3.org/2003/01/geo/wgs84_pos#> ");
                sb.AppendLine("PREFIX   foaf:   <http://xmlns.com/foaf/0.1/> ");
                sb.AppendLine("PREFIX   dbpedia-owl:    <http://dbpedia.org/ontology/> ");
                sb.AppendLine("PREFIX   dbpprop: <http://dbpedia.org/property/> ");
                sb.AppendLine("PREFIX   dbpedia: <http://dbpedia.org/resource/> ");

                sb.AppendLine(string.Empty);

                return sb.ToString();
            }
        }

        public const string DateTimeFormat = "u";
        public const string DateFormat = "yyyy-MM-dd";

        #region Constants

        public static Uri DateTimeDataType
        {
            get { return new Uri(Predicate.XsdDateTime); }
        }

        public static Uri DateDataType
        {
            get { return new Uri(Predicate.XsdDate); }
        }

        public static Uri IntegerDataType
        {
            get { return new Uri(Predicate.XsdInteger); }
        }

        public static Uri DecimalDataType
        {
            get { return new Uri(Predicate.XsdDecimal); }
        }

        public static string LanguageSlovenian
        {
            get { return "SL"; }
        }

        public static string LanguageEnglish
        {
            get { return "EN"; }
        }

        #endregion Constants

        #region Url Patterns

        public static string BaseUrl
        {
            get { return string.Format("http://{0}/mmc/", RtvSloConfig.RepositoryDomainName); }
        }

        public static string SiteUrl
        {
            get { return "mmc:sites/rtvslo"; }
        }

        public static string JournalistRoleUrl
        {
            get { return "mmc:roles/journalistAtRtvslo"; }
        }

        public static string ReaderRoleUrl
        {
            get { return "mmc:roles/readerAtRtvslo"; }
        }

        public static string GenderMaleUrl
        {
            get { return "mmc:genders/male"; }
        }

        public static string GenderFemaleUrl
        {
            get { return "mmc:genders/female"; }
        }

        public static string CategoryUrlPattern
        {
            get { return "mmc:categories/rtvslo_{0}"; }
        }

        public static string PostUrlPattern
        {
            get { return "mmc:posts/{0}"; }
        }

        public static string StatisticsUrlPattern
        {
            get { return "mmc:stats/{0}"; }
        }

        public static string CommentUrlPattern
        {
            get { return "mmc:comments/{0}"; }
        }

        public static string UserUrlPattern
        {
            get { return "mmc:users/{0}"; }
        }

        public static string UserStatisticsUrlPattern
        {
            get { return "mmc:user-statistics/{0}"; }
        }

        #endregion Url Patterns

        #region Public Methods

        public static string CreateQuery(string query)
        {
            return string.Format("{0} {1}", RepositoryHelper.Prefixes, query);
        }

        /// <summary>
        /// Create rdf:type, rdfs:Domain and rdfs:Range triples
        /// </summary>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="type"></param>
        /// <param name="domain"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static IList<Triple> CreateTypeDomainRangeTriples(IGraph g, string subject, string type, string domain, string range)
        {
            return new List<Triple>(){
                RepositoryHelper.CreateTypeTriple(g, subject, type),
                RepositoryHelper.CreateDomainTriple(g, subject, domain),
                RepositoryHelper.CreateRangeTriple(g, subject, range)
            };
        }

        /// <summary>
        /// Create rdf:type and rdfs:Domain triples
        /// </summary>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="type"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static IList<Triple> CreateTypeDomainTriples(IGraph g, string subject, string type, string domain)
        {
            return new List<Triple>(){
                RepositoryHelper.CreateTypeTriple(g, subject, type),
                RepositoryHelper.CreateDomainTriple(g, subject, domain)
            };
        }

        /// <summary>
        /// Create rdf:type triple
        /// </summary>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Triple CreateTypeTriple(IGraph g, string subject, string type)
        {
            return new Triple(subject.ToUriNode(g), Predicate.RdfType.ToUriNode(g), type.ToUriNode(g));
        }

        /// <summary>
        /// Create rdfs:Domain triple
        /// </summary>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="domain"></param>
        /// <returns></returns>
        public static Triple CreateDomainTriple(IGraph g, string subject, string domain)
        {
            return new Triple(subject.ToUriNode(g), Predicate.RdfsDomain.ToUriNode(g), domain.ToUriNode(g));
        }

        /// <summary>
        /// Create rdfs:Range triple
        /// </summary>
        /// <param name="g"></param>
        /// <param name="subject"></param>
        /// <param name="range"></param>
        /// <returns></returns>
        public static Triple CreateRangeTriple(IGraph g, string subject, string range)
        {
            return new Triple(subject.ToUriNode(g), Predicate.RdfsRange.ToUriNode(g), range.ToUriNode(g));
        }

        #endregion Public Methods
    }
}
