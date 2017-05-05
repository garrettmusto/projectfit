﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using SQLite;
using ProjectFit.Resources;

namespace ProjectFit
{
    [Activity(Label = "Edit Workout")]
    public class EditWorkoutActivity : Activity
    {
        private List<WorkoutStep> currentSteps;
        private List<WorkoutStep> originalSteps;
        private WorkoutStep editWorkoutStep;
        private Workout thisWorkout;
        private ListView stepsListView;
        private SQLiteConnection db;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.NewWorkoutBase);

            Button addNewExerciseButton = FindViewById<Button>(Resource.Id.btnAddNewExercise);
            Button doneNewExerciseButton = FindViewById<Button>(Resource.Id.btnDoneAddingExercise);
            TextView workoutBaseTextView = FindViewById<TextView>(Resource.Id.workoutBaseName);
            stepsListView = FindViewById<ListView>(Resource.Id.newWorkoutBaseExerciseList);
            stepsListView.ItemLongClick += StepsListView_ItemLongClick;
            currentSteps = new List<WorkoutStep>();

            var workoutId = Intent.GetIntExtra("workoutId",0);

            db = new SQLiteConnection(DbHelper.GetLocalDbPath());
            thisWorkout = db.Get<Workout>(workoutId);
            currentSteps = db.Table<WorkoutStep>().Where(x => x.WorkoutId == workoutId).ToList();
            originalSteps = db.Table<WorkoutStep>().Where(x => x.WorkoutId == workoutId).ToList();
            NewStepAdded();

            
            addNewExerciseButton.Click += AddNewExerciseButton_Click;
            doneNewExerciseButton.Click += DoneNewExerciseButtonOnClick;
        }

        private void StepsListView_ItemLongClick(object sender, AdapterView.ItemLongClickEventArgs e)
        {
            AlertDialog.Builder alert = new AlertDialog.Builder(this);
            alert.SetTitle("Confirm Delete");
            alert.SetMessage("Are you sure you want to delete this exercise from your workout?");
            alert.SetPositiveButton("Delete", (senderAlert, args) =>
            {
                db.Delete<WorkoutStep>(currentSteps[e.Position].Id);
                currentSteps.RemoveAt(e.Position);
                NewStepAdded();
            });
            alert.SetNegativeButton("Cancel", (senderAlert, args) =>
            {

            });

            Dialog dialog = alert.Create();
            dialog.Show();
        }

        private void DoneNewExerciseButtonOnClick(object sender, EventArgs eventArgs)
        {
            if (currentSteps.Count > 0)
            {
                foreach(var step in originalSteps)
                {
                    db.Delete<WorkoutStep>(step.Id);
                }
                currentSteps.RemoveAll(x =>x.ExerciseId == 0);
                currentSteps.Add(new WorkoutStep
                {
                    ExerciseId = 0,
                    WorkoutId = currentSteps[0].WorkoutId,
                    Reps = 0,
                    Sets = 0
                });
                
                foreach (var step in currentSteps)
                {
                    step.WorkoutId = thisWorkout.Id;
                    db.Insert(step);
                }

                db.Close();
            }

            Finish();
        }

        private void AddNewExerciseButton_Click(object sender, EventArgs e)
        {
            var selectExercise = new Intent(this, typeof(SelectExercisesActivity));

            var workoutName = thisWorkout.Name;

            selectExercise.PutExtra("workoutName", workoutName);

            StartActivityForResult(selectExercise, 1);

        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == 1)
            {
                if (resultCode == Result.Ok)
                {
                    var reps = data.GetIntExtra("reps", 0);
                    var sets = data.GetIntExtra("sets", 1);
                    var exerciseId = data.GetIntExtra("exerciseId", 0);

                    currentSteps.Add(new WorkoutStep
                    {
                        ExerciseId = exerciseId,
                        Reps = reps,
                        Sets = sets,
                    });

                    NewStepAdded();
                }
            }
        }

        private void NewStepAdded()
        {

            List<WorkoutStepDisplay> displaySteps = new List<WorkoutStepDisplay>();
            foreach (var workoutStep in currentSteps)
            {
                displaySteps.Add(new WorkoutStepDisplay
                {
                    Exercise = db.Get<Exercise>(workoutStep.ExerciseId),
                    Reps = workoutStep.Reps,
                    Sets = workoutStep.Sets
                });
            }

            var adapter = new NewWorkoutStepListAdapter(this, displaySteps.Where(x => x.Exercise.ExerciseId > 0).ToList());
            stepsListView.Adapter = adapter;
        }
    }
}