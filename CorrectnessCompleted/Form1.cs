using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using CSALMongo;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CorrectnessCompleted
{
    public partial class Form1 : Form
    {
        //should change the name of the database
        public const string DB_URL = "mongodb://localhost:27017/csaldata";
        //protected MongoDatabase testDB = null;
        List<String> studentsInClass = new List<string>();

        List<String> needStudents = new List<string>();
        //List<String> needStudentsA = new List<string>();

        public Form1()
        {
            InitializeComponent();

            String tags = "Lessons\tMedium Trial\tMedium Completion\tMedium Correctness\tEasy Trial\tEasy Completion\tEasy Correctness\tHard Trial\tHard Completion\tHard Correctness\tMedium2 Trial\tMedium2 Completion\tMedium2 Correctness\tLevel2 Trial\tLevel2 Completion\tLevel2 Correctness\n";

            this.richTextBox1.Text = tags;


            // Start 
            try
            {
                //query from the database
                var db = new CSALDatabase(DB_URL);
                var students = db.FindStudents();
                var lessons = db.FindLessons();
                var classes = db.FindClasses();

                // find the target classes
                foreach (var oneClass in classes)
                {
                    if (oneClass.ClassID == "pilot1_lai1" || oneClass.ClassID == "pilot1_lai2" || oneClass.ClassID == "pilot1_marietta1" || oneClass.ClassID == "pilot1_marietta2" ||
                        oneClass.ClassID == "pilot1_ptp1" || oneClass.ClassID == "pilot1_ptp1" || oneClass.ClassID == "pilot1_aecn" || oneClass.ClassID == "pilot1_tlp")
                    {
                        foreach (String student in oneClass.Students)
                        {
                            if (!String.IsNullOrWhiteSpace(student) && !String.IsNullOrWhiteSpace(oneClass.ClassID))
                            {
                                String cS = oneClass.ClassID + "-" + student;
                                needStudents.Add(cS);
                            }
                        }
                    }

                }

                // pre fill the matrix
                List<List<double>> triedLesson = fillMetrix(36, 6);
                List<List<double>> lessonCompletion = fillMetrix(36, 6);
                List<List<double>> lessonCompletionNum = fillMetrix(36, 6);
                List<List<double>> lessonCorrectness = fillMetrix(36, 6);

                foreach (var studentRecord in needStudents)
                {
                    List<String> perLesson = new List<string>();
                    if (!String.IsNullOrWhiteSpace(studentRecord))
                    {
                        // need to be careful here, may not right
                        perLesson = getInfoForOneStudent(studentRecord);
                    }

                    triedLesson = calculateTried(perLesson, triedLesson);
                    lessonCompletionNum = calculateCompletion(perLesson, lessonCompletionNum);
                    lessonCorrectness = calculateCorrectness(perLesson, lessonCorrectness);
                }

                // foreach lesson and foreach section
                for (int i = 1; i < 35; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        if (triedLesson[i][j] == 0 || lessonCompletionNum[i][j] == 0)
                        {
                            continue;
                        }
                        else
                        {
                            // calculate
                            lessonCompletion[i][j] = lessonCompletionNum[i][j] / triedLesson[i][j];
                            lessonCorrectness[i][j] = lessonCorrectness[i][j] / lessonCompletionNum[i][j];
                        }
                    }
                }

                String allString = getCorrectFormat(triedLesson, lessonCompletion, lessonCorrectness);

                this.richTextBox1.AppendText(allString);

            }
            catch (Exception e)
            {
                e.GetBaseException();
                e.GetType();

            }
        }

        public String getCorrectFormat(List<List<double>> triedStudent, List<List<double>> lessonCompletion, List<List<double>> lessonCorrectness)
        {
            String correctFormat = "";

            // foreach lesson get the correct format 
            // seperate each section
            for (int i = 1; i < 35; i++)
            {
                correctFormat += "lesson" + i.ToString() + "\t";
                for (int j = 0; j < 5; j++)
                {
                    correctFormat += triedStudent[i][j] + "\t" + lessonCompletion[i][j] + "\t" + lessonCorrectness[i][j] + "\t";
                }
                correctFormat += "\n";
            }

            return correctFormat;
        }

        // all the information
        public List<String> getDetailInfo(List<String> studentsList)
        {
            List<String> allText = new List<String>();

            // try to get Detail Info
            foreach (var studentRecord in studentsList)
            {
                if (!String.IsNullOrWhiteSpace(studentRecord))
                {
                    // need to be careful here, may not right
                    List<String> perLesson = getInfoForOneStudent(studentRecord);

                    if (perLesson == null || perLesson.Count < 1)
                    {
                        continue;
                    }
                    else
                    {
                        foreach (String perRecord in perLesson)
                        {
                            allText.Add(perRecord);
                        }
                    }

                    //allText += recordCount.ToString() + "\t" + getInfoForOne(studentRecord) + "\n";
                }
            }

            return allText;
        }

        // for every student
        public List<List<double>> calculateTried(List<String> allrecords, List<List<double>> triedLesson)
        {
            List<String> lessons = new List<string>();

            // lessonNum, sectionLevel
            List<List<double>> triedStudents = fillMetrix(36, 6);

            foreach (String perSection in allrecords)
            {
                int lessonNum = Int32.Parse(perSection.Split(new Char[] { '-' })[0]);
                int sectionLevel = Int32.Parse(perSection.Split(new Char[] { '-' })[1]);
                double correctness = Double.Parse(perSection.Split(new Char[] { '-' })[2]);

                if (correctness != 0)
                {
                    if (triedStudents[lessonNum][sectionLevel] == 0)
                    {
                        triedLesson[lessonNum][sectionLevel] += 1;
                        triedStudents[lessonNum][sectionLevel] += 1;
                    }
                }

            }

            return triedLesson;
        }


        // for every student
        public List<List<double>> calculateCompletion(List<String> allrecords, List<List<double>> lessonCompletion)
        {
            List<String> lessons = new List<string>();

            // lessonNum, sectionLevel
            List<List<double>> triedStudents = fillMetrix(36, 6);

            foreach (String perSection in allrecords)
            {
                int lessonNum = Int32.Parse(perSection.Split(new Char[] { '-' })[0]);
                int sectionLevel = Int32.Parse(perSection.Split(new Char[] { '-' })[1]);
                double correctness = Double.Parse(perSection.Split(new Char[] { '-' })[2]);

                if (correctness != 0 && correctness != 99)
                {
                    if (triedStudents[lessonNum][sectionLevel] == 0)
                    {
                        triedStudents[lessonNum][sectionLevel] += 1;
                        lessonCompletion[lessonNum][sectionLevel] += 1;
                    }
                }

            }

            return lessonCompletion;
        }

        // for every student
        public List<List<double>> calculateCorrectness(List<String> allrecords, List<List<double>> lessonCorrectness)
        {
            List<String> lessons = new List<string>();

            // lessonNum, sectionLevel
            List<List<double>> triedStudents = fillMetrix(36, 6);

            foreach (String perSection in allrecords)
            {
                int lessonNum = Int32.Parse(perSection.Split(new Char[] { '-' })[0]);
                int sectionLevel = Int32.Parse(perSection.Split(new Char[] { '-' })[1]);
                double correctness = Double.Parse(perSection.Split(new Char[] { '-' })[2]);

                if (correctness != 0 && correctness != 99)
                {
                    if (triedStudents[lessonNum][sectionLevel] == 0)
                    {
                        triedStudents[lessonNum][sectionLevel] += 1;
                        lessonCorrectness[lessonNum][sectionLevel] += correctness;
                    }
                }

            }

            return lessonCorrectness;
        }
        // for another format
        public List<String> getInfoForOneStudent(String studentRecord)
        {
            List<String> record = new List<string>();
            List<String> records = new List<string>();

            String classID = studentRecord.Split(new Char[] { '-' })[0];
            String subjectID = studentRecord.Split(new Char[] { '-' })[1];

            List<List<double>> allQues = fillMetrix(36, 6);

            for (int i = 1; i < 35; i++)
            {
                // all lessons
                record = getPerRecord3(subjectID, i);
                if (record != null)
                {
                    foreach (String perSection in record)
                    {
                        // might have multiple trial, need to pick from these
                        // foreach section, pick one with correctness, if there is no, then give section name, 0
                        int lessonNum = Int32.Parse(perSection.Split(new Char[] { '-' })[0]);
                        int sectionLevel = Int32.Parse(perSection.Split(new Char[] { '-' })[1]);
                        double correctness = Double.Parse(perSection.Split(new Char[] { '-' })[2]);

                        if (correctness != 0)
                        {
                            if (allQues[lessonNum][sectionLevel] == 0)
                            {
                                allQues[lessonNum][sectionLevel] = correctness;
                            }
                        }
                    }
                }
            }


            foreach (List<double> i in allQues)
            {
                foreach (double j in i)
                {
                    records.Add(allQues.IndexOf(i).ToString() + "-" + i.IndexOf(j).ToString() + "-" + j.ToString());
                }
            }

            return records;
        }


        public List<List<double>> fillMetrix(int row, int column)
        {
            List<List<double>> needMetrix = new List<List<double>>();
            for (int i = 1; i < row; i++)
            {
                List<double> rowName = new List<double>();
                for (int j = 1; j < column; j++)
                {
                    rowName.Add(0);
                }
                needMetrix.Add(rowName);
            }
            return needMetrix;
        }

        // get all data for each lesson with 3 levels
        // remain calculation for all the lessons
        // need to be added
        // if a student didn't finish the section but tried, will add 99
        public List<String> getPerRecord3(String studentName, int lessonNum)
        {
            String lessonID = "lesson" + lessonNum.ToString();
            var db = new CSALDatabase(DB_URL);
            var oneTurn = db.FindTurns(lessonID, studentName);
            int lastTurnID = 99, mediumQuesNum = 0, mediumScore = 0, sectionFlag = 0, medium2QN = 0, medium2QS = 0,
                easyQuesNum = 0, easyScore = 0, hardQuesNum = 0, hardScore = 0, secondLevelQN = 0, secondLevelQS = 0;
            Boolean finishMed = false, finishEasy = false, finishHard = false, finishM2 = false, finishL2 = false, skip = false;
            String correctness = "", correctEasy = "", correctHard = "", correctL2 = "", correctM2 = "";

            // for lesson 23
            Boolean reachAskQ = false, reachAnyQ = false;
            // for lesson 25
            Boolean firstAttempt = false;
            // for lesson 15, 28
            Boolean getAnswer = false;
            // for lesson 11, 18, 26
            int attempt = 0;


            List<String> quesInfo = new List<String>();

            if (oneTurn == null || oneTurn.Count < 1 || oneTurn[0].Turns.Count < 1)
            {
                return null;
            }
            else
            {
                foreach (var turn in oneTurn[0].Turns)
                {
                    if (turn.TurnID < lastTurnID)
                    {
                        if (correctness != "")
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + correctness);
                        }
                        else if (mediumQuesNum != 0)
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + "0-99");
                        }
                        if (correctEasy != "")
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + correctEasy);
                        }
                        else if (easyQuesNum != 0)
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + "1-99");
                        }
                        if (correctHard != "")
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + correctHard);
                        }
                        else if (hardQuesNum != 0)
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + "2-99");
                        }
                        if (correctM2 != "")
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + correctM2);
                        }
                        else if (medium2QN != 0)
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + "3-99");
                        }
                        if (correctL2 != "")
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + correctL2);
                        }
                        else if (secondLevelQN != 0)
                        {
                            quesInfo.Add(lessonNum.ToString() + "-" + "4-99");
                        }

                        sectionFlag = 0;
                        mediumQuesNum = 0;
                        mediumScore = 0;
                        medium2QN = 0;
                        medium2QS = 0;
                        easyQuesNum = 0;
                        easyScore = 0;
                        hardQuesNum = 0;
                        hardScore = 0;
                        secondLevelQN = 0;
                        secondLevelQS = 0;
                    }
                    else
                    {
                        // lesson 2, 13, 14, 26, 27
                        // sectionFlag 4 means level2
                        if (lessonNum == 2 || lessonNum == 13 || lessonNum == 14 || lessonNum == 27)
                        {
                            foreach (var transition in turn.Transitions)
                            {
                                if (transition.RuleID.ToString().Contains("ChangePageTB") && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 4;
                                }
                                else if (sectionFlag == 4 && transition.RuleID == "End")
                                {
                                    finishL2 = true;
                                }
                                else if (sectionFlag == 0 && transition.RuleID == "End")
                                {
                                    finishMed = true;
                                }
                            }

                            // calculate score
                            if (turn.Input.Event == "Correct" && sectionFlag == 0 && finishMed == false)
                            {
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Incorrect" && sectionFlag == 0 && finishMed == false)
                            {
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 4 && finishL2 == false)
                            {
                                secondLevelQS++;
                                secondLevelQN++;
                            }
                            else if (turn.Input.Event == "Incorrect" && sectionFlag == 4 && finishL2 == false)
                            {
                                secondLevelQN++;
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishL2 == true & correctL2 == "")
                            {
                                correctL2 = "4".ToString() + "-" + ((float)secondLevelQS / (float)secondLevelQN).ToString();
                                break;
                            }
                        }
                        // specific rule in lesson3, incorrect1 and incorrect2 both means first attempt in each question
                        if (lessonNum == 3)
                        {
                            // correctness
                            if (turn.Input.Event == "Correct" && sectionFlag == 0)
                            {
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect1" || turn.Input.Event == "Incorrect2") && sectionFlag == 0)
                            {
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 1)
                            {
                                easyScore++;
                                easyQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect1" || turn.Input.Event == "Incorrect2") && sectionFlag == 1)
                            {
                                easyQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 2)
                            {
                                hardScore++;
                                hardQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect1" || turn.Input.Event == "Incorrect2") && sectionFlag == 2)
                            {
                                hardQuesNum++;
                            }

                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }

                                // section level
                                if (transition.RuleID == "GetTutoringPackEasy" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }
                                if (transition.RuleID == "GetTutoringPackHard" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                finishMed = false;
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }
                        }
                        else if (lessonNum == 15 || lessonNum == 28)
                        {
                            foreach (var transition in turn.Transitions)
                            {

                                if (transition.RuleID == "GetTutoringPackHard" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }
                                else if (transition.RuleID == "GetTutoringPackEasy" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }

                                // get the question
                                if (transition.RuleID == "HasItem")
                                {
                                    foreach (var action in transition.Actions)
                                    {
                                        if (action.Agent == "System" && action.Act == "Display")
                                        {
                                            if (sectionFlag == 0)
                                            {
                                                getAnswer = true;
                                            }
                                            else if (sectionFlag == 1)
                                            {
                                                getAnswer = true;
                                            }
                                            else if (sectionFlag == 2)
                                            {
                                                getAnswer = true;
                                            }
                                        }
                                    }
                                }

                                if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }

                            }

                            if (turn.Input.Event.ToString().Contains("Incorrect") && getAnswer == true)
                            {
                                getAnswer = false;
                                // medium level, odd question number, skip
                                if (sectionFlag == 0)
                                {
                                    mediumQuesNum++;
                                }
                                else if (sectionFlag == 1)
                                {
                                    easyQuesNum++;
                                }
                                else if (sectionFlag == 2)
                                {
                                    hardQuesNum++;
                                }
                            }

                            else if (turn.Input.Event == "Correct" && getAnswer == true)
                            {
                                // score & attempt
                                getAnswer = false;
                                if (sectionFlag == 0)
                                {
                                    mediumQuesNum++;
                                    mediumScore++;
                                }
                                else if (sectionFlag == 1)
                                {
                                    easyQuesNum++;
                                    easyScore++;
                                }
                                else if (sectionFlag == 2)
                                {
                                    hardQuesNum++;
                                    hardScore++;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                finishMed = false;
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }
                        }
                        //lesson 20, only have 2 level, Medium and level2
                        else if (lessonNum == 20)
                        {
                            if (skip == true)
                            {
                                skip = false;
                            }
                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 4 && transition.RuleID == "End")
                                {
                                    finishL2 = true;
                                }

                                // need to skip
                                if (transition.RuleID == "HintCorrect")
                                {
                                    skip = true;
                                }

                                // section level
                                if (transition.RuleID == "ChangeLevelHard" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 4;
                                }
                            }

                            // calculate score
                            if (turn.Input.Event == "Correct" && sectionFlag == 0 && finishMed == false && skip == false)
                            {
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Incorrect" && sectionFlag == 0 && finishMed == false && skip == false)
                            {
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 4 && skip == false)
                            {
                                secondLevelQN++;
                                secondLevelQS++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 4 && skip == false)
                            {
                                secondLevelQN++;
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishL2 == true && correctL2 == "")
                            {
                                correctL2 = "4".ToString() + "-" + ((float)secondLevelQS / (float)secondLevelQN).ToString();
                            }
                        }

                        // lesson 22
                        else if (lessonNum == 22)
                        {
                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }

                                if (transition.StateID == "AskQ")
                                {
                                    reachAskQ = true;
                                }

                                // section level
                                if (transition.RuleID == "TPEasy" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }
                                if (transition.RuleID == "TPDifficult" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }
                            }

                            // calculate score
                            if (turn.Input.Event == "Correct" && sectionFlag == 0 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Incorrect" && sectionFlag == 0 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 1 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                easyScore++;
                                easyQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 1 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                easyQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 2 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                hardScore++;
                                hardQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 2 && reachAskQ == true)
                            {
                                reachAskQ = false;
                                hardQuesNum++;
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }

                        }

                        else if (lessonNum == 24)
                        {
                            if (skip == true)
                            {
                                skip = false;
                            }
                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }

                                // section level
                                if (transition.RuleID == "ChangeLevelEasy" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }
                                if (transition.RuleID == "ChangeLevelHard" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }

                                // calculate score
                                if (transition.RuleID == "Correct" && sectionFlag == 0)
                                {
                                    mediumScore++;
                                    mediumQuesNum++;
                                }
                                else if (transition.RuleID == "Incorrect" && sectionFlag == 0)
                                {
                                    mediumQuesNum++;
                                }
                                else if (transition.RuleID == "Correct" && sectionFlag == 1)
                                {
                                    easyScore++;
                                    easyQuesNum++;
                                }
                                else if ((transition.RuleID == "Incorrect") && sectionFlag == 1)
                                {
                                    easyQuesNum++;
                                }
                                else if (transition.RuleID == "Correct" && sectionFlag == 2)
                                {
                                    hardScore++;
                                    hardQuesNum++;
                                }
                                else if ((transition.RuleID == "Incorrect") && sectionFlag == 2)
                                {
                                    hardQuesNum++;
                                }
                            }


                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }
                        }
                        // need to be fixed
                        else if (lessonNum == 5 || lessonNum == 25)
                        {
                            foreach (var transition in turn.Transitions)
                            {
                                if (transition.RuleID == "GetEasyTutoringPack")
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }
                                else if (transition.RuleID == "GetHardTutoringPack")
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }
                                else if (transition.RuleID == "GetMediumTutoringPack")
                                {
                                    sectionFlag = 0;
                                }

                                if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }
                            }
                            if (turn.Input.Event == "Incorrect" || turn.Input.Event == "Incorrect1" || turn.Input.Event == "Incorrect2")
                            {

                                if (sectionFlag == 0)
                                {
                                    mediumQuesNum++;
                                }
                                else if (sectionFlag == 1)
                                {
                                    easyQuesNum++;
                                }
                                else if (sectionFlag == 2)
                                {
                                    hardQuesNum++;
                                }
                            }
                            else if (turn.Input.Event == "Correct")
                            {
                                if (firstAttempt == false)
                                {
                                    // score & attempt
                                    if (sectionFlag == 0)
                                    {
                                        mediumQuesNum++;
                                        mediumScore++;
                                    }
                                    else if (sectionFlag == 1)
                                    {
                                        easyQuesNum++;
                                        easyScore++;
                                    }
                                    else if (sectionFlag == 2)
                                    {
                                        hardQuesNum++;
                                        hardScore++;
                                    }
                                }
                                else
                                {
                                    firstAttempt = false;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }
                        }

                        // lesson 4, 7, 9, 16, 30
                        // lesson 4: one student only answered 3 in the medium level then went to hard
                        if (lessonNum == 4 || lessonNum == 7 || lessonNum == 9 || lessonNum == 16 || lessonNum == 29 || lessonNum == 30)
                        {
                            // correctness
                            if (turn.Input.Event == "Correct" && sectionFlag == 0)
                            {
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Incorrect" && sectionFlag == 0)
                            {
                                mediumQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 1)
                            {
                                easyScore++;
                                easyQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 1)
                            {
                                easyQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 2)
                            {
                                hardScore++;
                                hardQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 2)
                            {
                                hardQuesNum++;
                            }
                            else if (turn.Input.Event == "Correct" && sectionFlag == 3)
                            {
                                medium2QN++;
                                medium2QS++;
                            }
                            else if ((turn.Input.Event == "Incorrect") && sectionFlag == 3)
                            {
                                medium2QN++;
                            }

                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 1 && turn.Input.Event.ToString().Contains("Level2_Diagnostic"))
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 1 && transition.RuleID == "End")
                                {
                                    finishEasy = true;
                                }
                                else if (sectionFlag == 2 && turn.Input.Event.ToString().Contains("Level2_Diagnostic"))
                                {
                                    finishHard = true;
                                }
                                else if (sectionFlag == 2 && transition.RuleID == "End")
                                {
                                    finishHard = true;
                                }
                                else if (sectionFlag == 3 && transition.RuleID == "End")
                                {
                                    finishM2 = true;
                                }

                                // section level
                                if (transition.RuleID == "GetTutoringPackEasy" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 1;
                                }
                                if (transition.RuleID == "GetTutoringPackHard" && sectionFlag == 0)
                                {
                                    finishMed = true;
                                    sectionFlag = 2;
                                }
                                if (turn.Input.Event.ToString().Contains("Level2_Diagnostic"))
                                {
                                    sectionFlag = 3;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                            else if (finishEasy == true && correctEasy == "")
                            {
                                correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();

                            }
                            else if (finishHard == true && correctHard == "")
                            {
                                correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                            }
                            else if (finishM2 == true && correctM2 == "")
                            {
                                correctM2 = "3".ToString() + "-" + ((float)medium2QS / (float)medium2QN).ToString();
                            }
                        }

                        // lesson 11, 18, 26
                        else if (lessonNum == 11 || lessonNum == 18 || lessonNum == 26)
                        {
                            foreach (var transition in turn.Transitions)
                            {
                                if (transition.RuleID.Contains("AskQ"))
                                {
                                    reachAskQ = true;
                                    int index = transition.RuleID.IndexOf("AskQ");
                                    string cleanQues = (index < 0)
                                        ? transition.RuleID
                                        : transition.RuleID.Remove(index, "AskQ".Length);

                                    attempt = Int32.Parse(cleanQues.Split(new Char[] { '.' })[1]);
                                    break;
                                }

                                if (transition.RuleID == "End")
                                {
                                    finishMed = true;
                                }
                            }

                            if (turn.Input.Event.Contains("Correct") && reachAskQ == true)
                            {
                                reachAskQ = false;
                                if (attempt == 1)
                                {
                                    mediumQuesNum++;
                                    mediumScore++;
                                }
                            }
                            else if (turn.Input.Event.Contains("Incorrect") && reachAskQ == true)
                            {
                                reachAskQ = false;
                                if (attempt == 1)
                                {
                                    mediumQuesNum++;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                            }
                        }
                        else if (lessonNum == 31 || lessonNum == 32 || lessonNum == 33 || lessonNum == 34)
                        {
                            // correctness
                            if (turn.Input.Event == "Correct" && sectionFlag == 0)
                            {
                                mediumScore++;
                                mediumQuesNum++;
                            }
                            else if ((turn.Input.Event == "Incorrect1" || turn.Input.Event == "Incorrect2") && sectionFlag == 0)
                            {
                                mediumQuesNum++;
                            }

                            foreach (var transition in turn.Transitions)
                            {
                                if (sectionFlag == 0 && transition.RuleID == "End")
                                {
                                    finishMed = true;
                                }
                            }

                            if (finishMed == true && correctness == "")
                            {
                                correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                finishMed = false;
                            }
                        }
                        else
                        {
                            foreach (var transition in turn.Transitions)
                            {
                                // lesson 1, 5, 6, 10
                                if (lessonNum == 1 || lessonNum == 5 || lessonNum == 6 || lessonNum == 10 || lessonNum == 12 ||
                                    lessonNum == 19 || lessonNum == 21)
                                {
                                    // correctness
                                    if ((transition.RuleID == "Correct" || transition.RuleID == "Correct1") && sectionFlag == 0)
                                    {
                                        mediumScore++;
                                        mediumQuesNum++;
                                    }
                                    else if ((transition.RuleID == "Incorrect" || transition.RuleID == "Incorrect1") && sectionFlag == 0)
                                    {
                                        mediumQuesNum++;
                                    }
                                    else if ((transition.RuleID == "Correct" || transition.RuleID == "Correct1") && sectionFlag == 1)
                                    {
                                        easyScore++;
                                        easyQuesNum++;
                                    }
                                    else if ((transition.RuleID == "Incorrect" || transition.RuleID == "Incorrect1") && sectionFlag == 1)
                                    {
                                        easyQuesNum++;
                                    }
                                    else if ((transition.RuleID == "Correct" || transition.RuleID == "Correct1") && sectionFlag == 2)
                                    {
                                        hardScore++;
                                        hardQuesNum++;
                                    }
                                    else if ((transition.RuleID == "Incorrect" || transition.RuleID == "Incorrect1") && sectionFlag == 2)
                                    {
                                        hardQuesNum++;
                                    }

                                    if (sectionFlag == 1 && transition.RuleID == "End")
                                    {
                                        finishEasy = true;
                                    }
                                    else if (sectionFlag == 2 && transition.RuleID == "End")
                                    {
                                        finishHard = true;
                                    }
                                    else if (sectionFlag == 0 && transition.RuleID == "End")
                                    {
                                        finishMed = true;
                                    }

                                    // section level
                                    if (transition.RuleID == "GetTutoringPackEasy" && sectionFlag == 0)
                                    {
                                        finishMed = true;
                                        sectionFlag = 1;
                                    }
                                    if (transition.RuleID == "GetTutoringPackHard" && sectionFlag == 0)
                                    {
                                        finishMed = true;
                                        sectionFlag = 2;
                                    }

                                    if (finishMed == true && correctness == "")
                                    {
                                        correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                        finishMed = false;
                                    }
                                    else if (finishEasy == true && correctEasy == "")
                                    {
                                        correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                                    }
                                    else if (finishHard == true && correctHard == "")
                                    {
                                        correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                                    }
                                }
                                if (lessonNum == 23)
                                {
                                    if (transition.StateID == "AskQ" && transition.RuleID == "UserAnswer")
                                    {
                                        reachAskQ = true;
                                        continue;
                                    }
                                    else if (transition.StateID == "AnyQ" || transition.StateID == "HintQ")
                                    {
                                        reachAnyQ = true;
                                    }
                                    // last question of Hard
                                    if (transition.RuleID == "IncorrectSummary" && transition.StateID == "AskQ")
                                    {
                                        if (sectionFlag == 0)
                                        {
                                            mediumQuesNum++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyQuesNum++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardQuesNum++;
                                        }
                                    }
                                    // last question of Hard
                                    else if (transition.RuleID == "CorrectSummary" && transition.StateID == "AskQ")
                                    {
                                        reachAskQ = false;
                                        if (sectionFlag == 0)
                                        {
                                            mediumQuesNum++;
                                            mediumScore++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyQuesNum++;
                                            easyScore++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardQuesNum++;
                                            hardScore++;
                                        }
                                    }
                                    // first attempt correct
                                    else if (transition.RuleID.Contains("Correct") && transition.StateID == "AskQ")
                                    {
                                        // score & attempt
                                        if (sectionFlag == 0)
                                        {
                                            mediumScore++;
                                            mediumQuesNum++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyQuesNum++;
                                            easyScore++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardQuesNum++;
                                            hardScore++;
                                        }
                                    }
                                    // first attempt incorrect
                                    else if (transition.RuleID.Contains("Incorrect") && reachAskQ == true)
                                    {
                                        reachAskQ = false;
                                        if (sectionFlag == 0)
                                        {
                                            mediumQuesNum++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyQuesNum++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardQuesNum++;
                                        }
                                    }

                                    if (sectionFlag == 1 && transition.RuleID == "End")
                                    {
                                        finishEasy = true;
                                    }
                                    else if (sectionFlag == 2 && transition.RuleID == "End")
                                    {
                                        finishHard = true;
                                    }
                                    if (transition.RuleID == "MediaLoadedEasy")
                                    {
                                        finishMed = true;
                                        sectionFlag = 1;
                                    }
                                    if (transition.RuleID == "MediaLoadedHard" || transition.RuleID == "MediaLoadedDifficult")
                                    {
                                        finishMed = true;
                                        sectionFlag = 2;
                                    }

                                    if (finishMed && correctness == "")
                                    {
                                        correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                    }
                                    else if (finishEasy == true && correctEasy == "")
                                    {
                                        correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();

                                    }
                                    else if (finishHard == true && correctHard == "")
                                    {
                                        correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                                    }
                                }

                                if (lessonNum == 8)
                                {
                                    //Analyze actions to see if they add user score
                                    foreach (var action in transition.Actions)
                                    {
                                        if (action.Act == "AddUserScore" && action.Data == "1")
                                        {
                                            mediumScore++;
                                            mediumQuesNum++;
                                            break;
                                        }
                                        else if (action.Act == "GetMediaFeedback" && action.Data == "SAGoodAnswer")
                                        {
                                            mediumQuesNum++;
                                            break;
                                        }
                                    }
                                    if (transition.RuleID == "End")
                                    {
                                        correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                    }
                                }

                                if (lessonNum == 17)
                                {
                                    // analyze the correct/incorrect of the question
                                    if (transition.RuleID == "Correct1" || transition.RuleID == "Correct2" || transition.RuleID == "Correct")
                                    {
                                        if (sectionFlag == 0)
                                        {
                                            mediumScore++;
                                            mediumQuesNum++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyScore++;
                                            easyQuesNum++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardScore++;
                                            hardQuesNum++;
                                        }
                                    }
                                    else if (transition.RuleID == "Incorrect" || transition.RuleID == "Incorrect1" || transition.RuleID == "Incorrect2")
                                    {
                                        if (sectionFlag == 0)
                                        {
                                            mediumQuesNum++;
                                        }
                                        else if (sectionFlag == 1)
                                        {
                                            easyQuesNum++;
                                        }
                                        else if (sectionFlag == 2)
                                        {
                                            hardQuesNum++;
                                        }
                                    }
                                    // get the section level
                                    if (transition.RuleID == "GetTutoringPackHard" && sectionFlag == 0)
                                    {
                                        finishMed = true;
                                        sectionFlag = 2;
                                    }

                                    if (transition.RuleID == "GetTutoringPackEasy" && sectionFlag == 0)
                                    {
                                        finishMed = true;
                                        sectionFlag = 1;
                                    }

                                    if (sectionFlag == 1 && transition.RuleID == "End")
                                    {
                                        finishEasy = true;
                                    }
                                    else if (sectionFlag == 2 && transition.RuleID == "End")
                                    {
                                        finishHard = true;
                                    }

                                    if (finishMed == true && correctness == "")
                                    {
                                        correctness = "0".ToString() + "-" + ((float)mediumScore / (float)mediumQuesNum).ToString();
                                    }
                                    else if (finishEasy == true && correctEasy == "")
                                    {
                                        correctEasy = '1'.ToString() + "-" + ((float)easyScore / (float)easyQuesNum).ToString();
                                    }
                                    else if (finishHard == true && correctHard == "")
                                    {
                                        correctHard = "2".ToString() + "-" + ((float)hardScore / (float)hardQuesNum).ToString();
                                    }
                                }
                            }
                        }
                    }

                    lastTurnID = turn.TurnID;
                }


                if (correctness != "")
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + correctness);
                }
                else if (mediumQuesNum != 0)
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + "0-99");
                }
                if (correctEasy != "")
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + correctEasy);
                }
                else if (easyQuesNum != 0)
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + "1-99");
                }
                if (correctHard != "")
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + correctHard);
                }
                else if (hardQuesNum != 0)
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + "2-99");
                }
                if (correctM2 != "")
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + correctM2);
                }
                else if (medium2QN != 0)
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + "3-99");
                }
                if (correctL2 != "")
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + correctL2);
                }
                else if (secondLevelQN != 0)
                {
                    quesInfo.Add(lessonNum.ToString() + "-" + "4-99");
                }
            }

            return quesInfo;
        }
    }
}