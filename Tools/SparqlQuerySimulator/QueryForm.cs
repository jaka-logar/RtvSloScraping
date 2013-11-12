using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using VDS.RDF.Storage;
using RtvSlo.Core.Configuration;
using VDS.RDF.Query;
using RtvSlo.Core.HelperExtensions;
using RtvSlo.Core.Helpers;

namespace SparqlQuerySimulator
{
    public partial class QueryForm : Form
    {
        public QueryForm()
        {
            InitializeComponent();

            queryTextBox.Text = RepositoryHelper.Prefixes;
        }

        private void queryButton_Click(object sender, EventArgs e)
        {
            DisableInput();

            using (SesameHttpProtocolConnector connector = new SesameHttpProtocolConnector(RtvSloConfig.RepositoryUrl, RtvSloConfig.RepositoryName))
            {
                if (connector.IsReady)
                {
                    try
                    {
                        SparqlResultSet queryResult = connector.Query(queryTextBox.Text) as SparqlResultSet;

                        if (queryResult != null)
                        {
                            resultTextBox.Text = queryResult.Results.SerializeObject();
                        }
                        else
                        {
                            resultTextBox.Text = queryResult.SerializeObject();
                        }
                    }
                    catch (Exception ex)
                    {
                        resultTextBox.Text = ex.Message;
                    }
                    finally
                    {
                        EnableInput();
                    }
                }

            }

        }

        private void DisableInput()
        {
            queryTextBox.Enabled = false;
            queryButton.Enabled = false;
            resultTextBox.Enabled = false;
        }

        private void EnableInput()
        {
            queryTextBox.Enabled = true;
            queryButton.Enabled = true;
            resultTextBox.Enabled = true;
        }
    }
}
