using MaterialSkin;
using MaterialSkin.Controls;
using System.Text;

namespace MaterialSkinExample
{
    public partial class MainForm : MaterialForm
    {
        private readonly MaterialSkinManager materialSkinManager;

        public MainForm()
        {
            InitializeComponent();

            // Initialize MaterialSkinManager
            materialSkinManager = MaterialSkinManager.Instance;

            // Set this to false to disable backcolor enforcing on non-materialSkin components
            // This HAS to be set before the AddFormToManage()
            materialSkinManager.EnforceBackcolorOnAllComponents = true;

            // MaterialSkinManager properties
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Indigo500, Primary.Indigo700, Primary.Indigo100, Accent.Pink200, TextShade.WHITE);

            // Add dummy data to the listview
            SeedListView();
            materialCheckedListBox1.Items.Add("Item1", false);
            materialCheckedListBox1.Items.Add("Item2", true);
            materialCheckedListBox1.Items.Add("Item3", true);
            materialCheckedListBox1.Items.Add("Item4", false);
            materialCheckedListBox1.Items.Add("Item5", true);
            materialCheckedListBox1.Items.Add("Item6", false);
            materialCheckedListBox1.Items.Add("Item7", false);

            materialComboBox6.SelectedIndex = 0;

            MaterialListBoxFormStyle.Clear();
            foreach (var FormStyleItem in Enum.GetNames<FormStyles>())
            {
                MaterialListBoxFormStyle.AddItem(FormStyleItem);
                if (FormStyleItem == FormStyle.ToString())
                {
                    MaterialListBoxFormStyle.SelectedIndex = MaterialListBoxFormStyle.Items.Count - 1;
                }
            }

            MaterialListBoxFormStyle.SelectedIndexChanged += (sender, args) =>
            {
                MaterialForm.FormStyles SelectedStyle = Enum.Parse<FormStyles>(args.Text);
                if (FormStyle != SelectedStyle)
                {
                    FormStyle = SelectedStyle;
                }
            };

            materialMaskedTextBox1.ValidatingType = typeof(System.Int16);

        }

        private void SeedListView()
        {
            //Define
            var data = new[]
            {
                new []{"Lollipop", "392", "0.2", "0"},
                new []{"KitKat", "518", "26.0", "7"},
                new []{"Ice cream sandwich", "237", "9.0", "4.3"},
                new []{"Jelly Bean", "375", "0.0", "0.0"},
                new []{"Honeycomb", "408", "3.2", "6.5"}
            };

            //Add
            foreach (string[] version in data)
            {
                var item = new ListViewItem(version);
                materialListView1.Items.Add(item);
            }
        }

        private void MaterialButton7_Click(object sender, EventArgs e)
        {
            materialSkinManager.Theme = materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? MaterialSkinManager.Themes.LIGHT : MaterialSkinManager.Themes.DARK;
            UpdateColor();
        }

        private int colorSchemeIndex;

        private void MaterialButton4_Click(object sender, EventArgs e)
        {
            colorSchemeIndex++;
            if (colorSchemeIndex > 2)
            {
                colorSchemeIndex = 0;
            }

            UpdateColor();
        }

        private void UpdateColor()
        {
            //These are just example color schemes
            switch (colorSchemeIndex)
            {
                case 0:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal500 : Primary.Indigo500,
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal700 : Primary.Indigo700,
                        materialSkinManager.Theme == MaterialSkinManager.Themes.DARK ? Primary.Teal200 : Primary.Indigo100,
                        Accent.Pink200,
                        TextShade.WHITE);
                    break;

                case 1:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        Primary.Green600,
                        Primary.Green700,
                        Primary.Green200,
                        Accent.Red100,
                        TextShade.WHITE);
                    break;

                case 2:
                    materialSkinManager.ColorScheme = new ColorScheme(
                        Primary.BlueGrey800,
                        Primary.BlueGrey900,
                        Primary.BlueGrey500,
                        Accent.LightBlue200,
                        TextShade.WHITE);
                    break;
            }
            Invalidate();
        }

        private void MaterialButton2_Click(object sender, EventArgs e)
        {
            materialProgressBar1.Value = Math.Min(materialProgressBar1.Value + 10, 100);
        }

        private void MaterialFlatButton2_Click(object sender, EventArgs e)
        {
            materialProgressBar1.Value = Math.Max(materialProgressBar1.Value - 10, 0);
        }

        private void MaterialSwitch4_CheckedChanged(object sender, EventArgs e)
        {
            DrawerUseColors = MaterialSwitch_Home1.Checked;
        }

        private void MaterialSwitch5_CheckedChanged(object sender, EventArgs e)
        {
            DrawerHighlightWithAccent = MaterialSwitch_Home2.Checked;
        }

        private void MaterialSwitch6_CheckedChanged(object sender, EventArgs e)
        {
            DrawerBackgroundWithAccent = MaterialSwitch_Home3.Checked;
        }

        private void MaterialSwitch8_CheckedChanged(object sender, EventArgs e)
        {
            DrawerShowIconsWhenHidden = MaterialSwitch_Home4.Checked;
        }

        private void MaterialButton3_Click(object sender, EventArgs e)
        {
            var builder = new StringBuilder("Batch operation report:\n\n");
            var random = new Random();
            for (int i = 0; i < 200; i++)
            {
                int result = random.Next(1000);
                if (result < 950)
                {
                    builder.AppendFormat(" - Task {0}: Operation completed sucessfully.\n", i);
                }
                else
                {
                    builder.AppendFormat(" - Task {0}: Operation failed! A very very very very very very very very very very very very serious error has occured during this sub-operation. The errorcode is: {1}).\n", i, result);
                }
            }

            _ = builder.ToString();
            string? batchOperationResults = "Simple text";
            _ = MaterialMessageBox.Show(batchOperationResults, "Batch Operation", MessageBoxButtons.YesNoCancel, FlexibleMaterialForm.ButtonsPosition.Center);
            materialComboBox1.Items.Add("this is a very long string");
        }

        private void MaterialSwitch9_CheckedChanged(object sender, EventArgs e)
        {
            DrawerAutoShow = MaterialSwitch_Home5.Checked;
        }

        private void MaterialTextBox2_LeadingIconClick(object sender, EventArgs e)
        {
            MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Leading Icon Click");
            SnackBarMessage.Show(this);

        }

        private void MaterialButton6_Click(object sender, EventArgs e)
        {
            MaterialSnackBar SnackBarMessage = new MaterialSnackBar("SnackBar started succesfully", "OK", true);
            SnackBarMessage.Show(this);
        }

        private void MaterialSwitch10_CheckedChanged(object sender, EventArgs e)
        {
            materialTextBox21.UseAccent = materialSwitch10.Checked;
        }

        private void MaterialSwitch11_CheckedChanged(object sender, EventArgs e)
        {
            materialTextBox21.UseTallSize = materialSwitch11.Checked;
        }

        private void MaterialSwitch12_CheckedChanged(object sender, EventArgs e)
        {
            if (materialSwitch12.Checked)
            {
                materialTextBox21.Hint = "Hint text";
            }
            else
            {
                materialTextBox21.Hint = "";
            }
        }

        private void MaterialComboBox7_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (materialComboBox7.SelectedIndex == 1)
            {
                materialTextBox21.PrefixSuffix = MaterialTextBox2.PrefixSuffixTypes.Prefix;
            }
            else if (materialComboBox7.SelectedIndex == 2)
            {
                materialTextBox21.PrefixSuffix = MaterialTextBox2.PrefixSuffixTypes.Suffix;
            }
            else
            {
                materialTextBox21.PrefixSuffix = MaterialTextBox2.PrefixSuffixTypes.None;
            }
        }

        private void MaterialSwitch13_CheckedChanged(object sender, EventArgs e)
        {
            materialTextBox21.UseSystemPasswordChar = materialSwitch13.Checked;

        }

        private void MaterialSwitch14_CheckedChanged(object sender, EventArgs e)
        {
            if (materialSwitch14.Checked)
            {
                materialTextBox21.LeadingIcon = Properties.Resources.baseline_fingerprint_black_24dp;
            }
            else
            {
                materialTextBox21.LeadingIcon = null;
            }
        }

        private void MaterialSwitch15_CheckedChanged(object sender, EventArgs e)
        {
            if (materialSwitch15.Checked)
            {
                materialTextBox21.TrailingIcon = Properties.Resources.baseline_build_black_24dp;
            }
            else
            {
                materialTextBox21.TrailingIcon = null;
            }
        }

        private void MaterialTextBox21_LeadingIconClick(object sender, EventArgs e)
        {
            MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Leading Icon Click");
            SnackBarMessage.Show(this);
        }

        private void MaterialTextBox21_TrailingIconClick(object sender, EventArgs e)
        {
            MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Trailing Icon Click");
            SnackBarMessage.Show(this);
        }

        private void MsReadOnly_CheckedChanged(object sender, EventArgs e)
        {
            materialCheckbox1.ReadOnly = msReadOnly.Checked;
        }

        private void MaterialButton25_Click(object sender, EventArgs e)
        {
            MaterialDialog materialDialog = new MaterialDialog(this, "Dialog Title", "Dialogs inform users about a task and can contain critical information, require decisions, or involve multiple tasks.", "OK", true, "Cancel");
            DialogResult result = materialDialog.ShowDialog(this);

            MaterialSnackBar SnackBarMessage = new MaterialSnackBar(result.ToString(), 750);
            SnackBarMessage.Show(this);

        }

        private void MaterialSwitch16_CheckedChanged(object sender, EventArgs e)
        {
            materialTextBox21.ShowAssistiveText = materialSwitch16.Checked;
        }

        private void MaterialButton26_Click(object sender, EventArgs e)
        {
            TabPage1.DrawIconSilhouette = !TabPage1.DrawIconSilhouette;
        }
    }
}
