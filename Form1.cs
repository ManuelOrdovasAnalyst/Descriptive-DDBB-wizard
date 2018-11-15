using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace DescribeMe
{
	public partial class Form1 : Form
	{
		Dictionary<string, int[]> groupLocals = new Dictionary<string, int[]>();

		int ngroups = 0;
		string[] columnnames;

		public Form1()
		{
			InitializeComponent();
			dataGridViewResults.Columns.Add("Statistic", "Statistic");
			dataGridViewResults.Columns.Add("Result","Result");
			dataGridViewResults.Columns["Result"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			foreach (DataGridViewColumn column in dataGridViewResults.Columns)
			{
				column.SortMode = DataGridViewColumnSortMode.NotSortable;
			}
		}

		// DOUBLE BUFFERED DATAGRID THANKS TO QUB1N:
		//https://stackoverflow.com/questions/4255148/how-to-improve-painting-performance-of-datagridview#4255299
		public class DataGridViewDoubleBuffered : DataGridView
		{
			public DataGridViewDoubleBuffered()
			{
				DoubleBuffered = true;
			}
		}

		// PERCENTILE CALCULATION
		public double Percentile(double[] sequence, double excelPercentile)
		{
			Array.Sort(sequence);
			int N = sequence.Length;
			double n = (N - 1) * excelPercentile + 1;
			if (n == 1d) return sequence[0];
			else if (n == N) return sequence[N - 1];
			else
			{
				int k = (int)n;
				double d = n - k;
				return sequence[k - 1] + d * (sequence[k] - sequence[k - 1]);
			}
		}

		//SEARCH FILE BUTTON
		private void button3_Click(object sender, EventArgs e)
		{
			OpenFileDialog openFileDialog1 = new OpenFileDialog();
			openFileDialog1.Filter = "Data files|*.dat";
			openFileDialog1.Title = "Select a data file";
			if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				dataGridView1.Columns.Clear();
				string text = System.IO.File.ReadAllText(openFileDialog1.FileName, Encoding.Default);
				string[] filas = text.Split('\n');
				columnnames = filas[0].ToUpper().Split('\t');
				columnnames[(columnnames.Length - 1)] = Regex.Replace(columnnames[(columnnames.Length - 1)], @"\t|\n|\r", "");

				//ADDING A COLUM FOR ROW INDEXES
				dataGridView1.Columns.Add("N", "N");
				string[] indices = new string[filas.Length];

				for (int i = 0; i < columnnames.Length; i++)
				{
					dataGridView1.Columns.Add(columnnames[i], columnnames[i]);
				}

				for (int i = 1; i < (filas.Length); i++)
				{
					string datosfila = filas[i];
					string[] vectorfila = ((i - 1).ToString() + '\t' + datosfila).Split('\t');
					if (vectorfila.Length - 1 == columnnames.Length)
					{
						this.dataGridView1.Rows.Add(vectorfila);
					}
				}
				DataGridViewColumn column = dataGridView1.Columns[0];
				column.Visible = false;
			}
		}

		//CUANTITATIVE ANALYSIS BUTTON
		private void cuantiButton_Click(object sender, EventArgs e)
		{
			if (ngroups == 0)
			{
				MessageBox.Show("No groups indicated!");
			}
			else
			{
				dataGridViewResults.Rows.Clear();
				dataGridViewResults.Refresh();
				int columnIndex = dataGridView1.CurrentCell.ColumnIndex;

				for (int i = 0; i < ngroups; i++)
				{
					//FIND CORRECT ROWS EVEN IF REORDERED
					int[] indicesIntini = groupLocals.Values.ToArray()[ngroups-1-i];
					int[] indicesInt = new int[indicesIntini.Length];
					int z = 0;

					for (int x = 0; x < dataGridView1.RowCount; x++)
					{

						for (int y = 0; y < indicesIntini.Length; y++)
						{

							if (Convert.ToInt16(dataGridView1.Rows[x].Cells[0].Value) == indicesIntini[y])
							{
								indicesInt[z] = x;
								z++;
							}
						}
					}

					int nsel = indicesInt.Length;

					if (nsel < 1)
					{
						MessageBox.Show("Group " + groupLocals.Keys.ToArray()[ngroups - 1 - i].ToString() +
							" has length less than 2!");
						break;
					}

					List<double> valores = new List<double>();

					foreach (int j in indicesInt)
					{
						string numVal;

						try
						{
							if (checkBox1.Checked)
							{
								numVal = dataGridView1.Rows[(j)].Cells[columnIndex].Value.ToString().Replace(".", ",");
							}
							else
							{
								numVal = dataGridView1.Rows[(j)].Cells[columnIndex].Value.ToString();
							}
						}
						catch (NullReferenceException)
						{
							numVal = "N/A";
						}

						if (double.TryParse(numVal, out double num))
						{
							valores.Add(Convert.ToDouble(numVal));
						}
					}

					double[] valores2 = valores.ToArray();

					if (valores2.Length < 1)
					{
						MessageBox.Show("Group " + groupLocals.Keys.ToArray()[ngroups - 1 - i].ToString() +
	" has less than 1 valid value");
						break;
					}

					double media = valores2.Sum() / valores2.Length;
					double[] diferenciascuad = new double[valores2.Length];
					for (int k = 0; k < valores2.Length; k++)
					{
						diferenciascuad[k] = Math.Pow((valores2[k] - media), 2);
					}

					double sdev = Math.Sqrt(diferenciascuad.Sum() / (valores2.Length - 1));
					double pcj = ((double)(nsel - (valores2.Length)) / (double)nsel) * 100;


					dataGridViewResults.Rows.Add("Group", groupLocals.Keys.ToArray()[ngroups - 1 - i].ToString());
					dataGridViewResults.Rows.Add("N Valid cases", valores2.Length.ToString());
					dataGridViewResults.Rows.Add("Mean (sd)", Math.Round(media, 2).ToString() + " (" + Math.Round(sdev, 2) + ")");
					dataGridViewResults.Rows.Add("Min", valores2.Min().ToString());
					dataGridViewResults.Rows.Add("Max", valores2.Max().ToString());
					dataGridViewResults.Rows.Add("P.25", Math.Round(Percentile(valores2, 0.25), 2).ToString());
					dataGridViewResults.Rows.Add("P.50", Math.Round(Percentile(valores2, 0.50), 2).ToString());
					dataGridViewResults.Rows.Add("P.75", Math.Round(Percentile(valores2, 0.75), 2).ToString());
					dataGridViewResults.Rows.Add("N/A", (nsel - (valores2.Length)).ToString() + " (" + Math.Round(pcj, 2).ToString() + "%)");
					dataGridViewResults.Rows.Add(" ", " ");
				}
			}
		}

		//CUALITATIVE ANALYSIS BUTTON
		private void cualiButton_Click(object sender, EventArgs e)
		{
			if (ngroups == 0)
			{
				MessageBox.Show("No groups indicated!");
			}
			else
			{
			   dataGridViewResults.Rows.Clear();
			   dataGridViewResults.Refresh();

				int columnIndex = dataGridView1.CurrentCell.ColumnIndex;
				for (int i = 0; i < ngroups; i++)
				{
					//FIND CORRECT ROWS EVEN IF REORDERED
					int[] indicesIntini = groupLocals.Values.ToArray()[ngroups - 1 - i];
					int[] indicesInt = new int[indicesIntini.Length];
					int z = 0;
					for (int x = 0; x < dataGridView1.RowCount; x++)
					{
						for (int y = 0; y < indicesIntini.Length; y++)
						{
							if (Convert.ToInt16(dataGridView1.Rows[x].Cells[0].Value) == indicesIntini[y])
							{
								indicesInt[z] = x;
								z++;
							}
						}
					}

					List<string> valores2 = new List<string>();

					foreach (int j in indicesInt)
					{
						try
						{
							valores2.Add(dataGridView1[columnIndex, j].Value.ToString().ToUpper());
						}
						catch (NullReferenceException)
						{
							valores2.Add("N/A");
						}
					}

					var frequency = valores2.GroupBy(x => x).ToDictionary(x => x.Key, x => x.Count());

					string[] myKeys = frequency.Keys.ToArray();
					int[] myValues = frequency.Values.ToArray();


					// i indicates revolutions

					dataGridViewResults.Rows.Add("Group", groupLocals.Keys.ToArray()[ngroups - 1 - i].ToString());
					dataGridViewResults.Rows.Add("N Valid cases", myValues.Sum().ToString());

					for (int j = 0; j < myKeys.Length; j++)
					{
						dataGridViewResults.Rows.Add(myKeys[j], myValues[j] + " (" + Math.Round((double)(myValues[j]) / (double)(myValues.Sum()) * 100, 2).ToString() + "%)");
					}
					dataGridViewResults.Rows.Add(" ", " ");
				}
			}
		}

		//SET GROUP BUTTON
		private void buttonGroup_Click(object sender, EventArgs e)
		{
			DataGridViewSelectedCellCollection seleccionados = dataGridView1.SelectedCells;

			//https://stackoverflow.com/questions/1674874/c-sharp-datagridview-and-selectedcells-finding-the-row-indexes-of-selected-c
			//Obtener las filas de las celdas seleccionadas
			int[] rowIndexesInit = (from sc in dataGridView1.SelectedCells.Cast<DataGridViewCell>()
									select sc.RowIndex).Distinct().ToArray();
			int[] rowIndexes = new int[rowIndexesInit.Length];

			int ip = 0;
			foreach (int element in rowIndexesInit)
			{
				rowIndexes[ip] = Convert.ToUInt16(dataGridView1.Rows[element].Cells[0].Value);
				ip++;
			}

			string groupname = textBoxGroup.Text.ToString();

			try
			{
				//thanks to Jan Remunda:
				//https://stackoverflow.com/questions/1822811/int-array-to-string#1822819
				string arrayIndexes = String.Join(",", new List<int>(rowIndexes).ConvertAll(i => i.ToString()).ToArray());

				groupLocals.Add(groupname, rowIndexes);

				ngroups++;
				labelGroup.Text = "N groups = " + ngroups.ToString();

				textBoxGroup.Text = "";


                dataGridViewResults.Rows.Add("'"+groupname+"' N =", rowIndexes.Length.ToString());
                
            }
			catch (ArgumentException)
			{
				MessageBox.Show("Repeated group name!");
			}

		}

		//SEARCH IN TITLE BUTTON
		private void button1_Click(object sender, EventArgs e)
		{
			if (textBox1.Text == "")
			{
				MessageBox.Show("No key provided!");
			}
			else
			{
				if (columnnames.Length == 0)
				{
					MessageBox.Show("No columns found!");
				}
				else
				{
					bool firstsearch = true;
					int i = 0;
					while (true)
					{
						string title = columnnames[i];
						bool found = title.Substring(0, title.Length).Contains(textBox1.Text.ToUpper());

						if (found)
						{
							if (!firstsearch)
							{
								if (MessageBox.Show("Continue?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
								{
                                    textBox1.Text = "";
									break;
								}
							}
							firstsearch = false;

							dataGridView1.ClearSelection();
							int selectedcol = i + 1;
							dataGridView1.FirstDisplayedScrollingColumnIndex = selectedcol;

                            for(int hey = 0; hey < columnnames.Length; hey++)
                            {
                                dataGridView1.Rows[hey].Cells[selectedcol].Selected = true;
                            }

                        }
						++i;
						if (i == columnnames.Length)
						{
							if (firstsearch == true)
							{
								MessageBox.Show("No coincidences found!");
								textBox1.Text = "";
								break;
							}
							i = 0;
						}
					}
				}
			}
		}

        //DELETE GROUPS BUTTON
        private void buttonResetGroup_Click(object sender, EventArgs e)
        {
            groupLocals = new Dictionary<string, int[]>();
            ngroups = 0;
            labelGroup.Text = "N groups = 0";
        }

        //DELETE TEXTBOX TEXT
        private void button4_Click(object sender, EventArgs e)
        {
            dataGridViewResults.Rows.Clear();
        }

        //UNUSED
        private void textBox2_TextChanged(object sender, EventArgs e) { }
		private void checkBox1_CheckedChanged(object sender, EventArgs e) { }
		private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e) { }
		private void openFileDialog1_FileOk(object sender, CancelEventArgs e) { }
		private void textBoxGroup_TextChanged(object sender, EventArgs e) { }
		private void labelGroup_Click(object sender, EventArgs e) { }
		private void textBox1_TextChanged(object sender, EventArgs e) { }
	}
}
