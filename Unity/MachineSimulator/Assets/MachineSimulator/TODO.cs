// - [X] cleanup code
// - [X] Add a thing that let's us quickly switch between which solution is used
// - [X] make our current arm work no matter how it is rotated (calculations need to be in local space)
// - [X] make our current arm work no matter where it is placed
// - [X] would be nice if we had gizmo lines for all joints maybe?
// - [X] also make upper joint move (keep this one parallel to floor)
// - [X] automatically instanciate arms at sensible positions
//     - [X] instantiate end effector platform on play
//     - [X] instantiate 6 arms on play (setup target positions inside end effector platform)
//     - [X] parameters for easy experimentation:
//           - distance from center for motors pairs at midpoint
//           - distance apart of motor pair
//           - distance from center for target pairs below endeffector at midpoint
//           - distance apart of target pair
// - [X] Fix joints-can't-handle-tilt-problem (need more degrees of freedom for top most joint)
//     - [X] add that debugGizmo method -> (origin, redDir, greenDir, blueDir)
//     - [X] pass in end effector (hexaplate) tilt
//     - [X] FirtStep: Calculate correct realTarget according to tilt
//     - [X] SecondStep: Calculate correct joint angles for joints 4 and 5 according to endEffector tilt
// - [X] Fix last joint not rotating together with tilted platform problem

// - [X] send 6 values instead of 4 to microcontroller
// - [X] handle 6 values and 6 sineSteppers on microcontroller
// - [X] add two more OUTPUTS (for the additional two motors) to the constants file on the microcontroller
// - [X] connect the 6 stepper controllers accordingly
// - [X] Add UI buttons to calibrate each motor (need to adjust rotation in small steps)
// - [X] Add _levelingOffset which is a LLMachineState that will accumulate calibration values for all the motors
// - [X] For normal move instructions, add them to the _levelingOffset to get the real target machine state
// - [X] Need to set the machineState after above calibration as the origin position for the machine
// - [X] All following diff-instructions should be relative to the origin position
// - [X] Add UI buttons to do the following: up/down at different speeds
// - [X] TEST WITH ARMS CONNECTED
// Things we now know after that test:
// - [X] test with NO-feedback-loop (no rotary encoder) stepper driver settings
// - [X] we probably need to put something soft under the arms on startup. Some arms are pushing agains the ground because of the way steppers work.
// - [X] Add apply-calibration button (applies all the values we found to be ideal for each motor)
// - [X] we also want a button that makes the machine go up/down continuously. maybe 10 times with pauses in between (maybe multiple pause intervals?)
// - [X] we want to go up/down even faster
// - [X] we want to go further up and down to a position above origin

// Continue work on this ↓
// - [ ] We need a way to move the simulatedMachine with HighLevelInstructions.
//     - [X] HighLevelInstruction needs to contain: PlateCenterPosition, PlateRotationQuaternion, MoveTime
//     - [X] We Need a "add pose" button
//     - [X] First goal will be to be able to play-back the recorded HighLevelInstruction
//     - [X] Introduce the concept of a "sequence" (a list of HighLevelInstructions). This will be useful when converting the HighLevelInstructions to stringed LowLevelInstructions.
//     - [ ] Next, create stringed LowLevelInstructions from the sequence (go through data in pairs: "from" HLInstruction, "to" HLInstruction).
//           - To generate them, we move time forward in small steps, check how far each of the motors has moved and create a LowLevelInstruction for each step.
// - [ ] The stringed together instructions can basically be executed as linear-speed movements to target pos. We DO of course use a sine-based movement when moving the end effector
//       when generating motor rotations with IK, but the movement commands themselves can be linear speed movements to target position because they will be very short and about 50 for a complete move.
//       The shortness and number of linear-speed moves will ensure that in totality, a smooth movement can be achieved.

// Thinking:
// - There's a slight problem with how the microcontroller handles moveCommands:
//     - The microcontroller remembers all the current motor positions and calculates the steps needed to reach a target rotation for each motor
//     - The problem is that with the slicing approach, if the machine isn't in the exact state we expect it to be in, the first moveCommand
//       might be at a way too high speed if the target position is too far away.
//     - I don't think there's a ideal solution to fix/improve this though.

// - [ ] test with different stepper driver PID settings
//     -> this might be interesting, especially if we connect the driver to the setup software and look through all the settings,
//        but I'm not sure we'll get anything out of this (going no-feedback-loop might be the best option after all)

// - [ ] NEXTUP: change colors of arm parts which are colliding so that we can get a better feeling of how the arm design has to be improved
// - [ ] add some sort of animation for end effector platform
//     - [X] moving up and down
//     - [X] moving left and right
//     - [X] moving forward and backward
//     - [X] circling (position)
//     - [X] circling + up/down (position)
//     - [X] tilting around the X-Axis
//     - [ ] tilting around the Z-Axis
//     - [ ] circling (tilt)
//     - [ ] moving up and down at left/right/front/back position
// - [ ] iterate on arm design based on what we'll see in the simulated machine
// - [ ] center gizmo would be nice, so we can tell how far away from center the hexaplate is
// - [ ] maybe switch project to the High Definition Render Pipeline (HDRP) with demo scene as base for better visual look
// - [ ] Need a name for the thing. Some ideas:
//   - u-joint. The UJ-Table
//   - hexapod. The Hexa Bot.

// Thing to check:
// - Wouldn't it also work if the 6 arms were placed one at a time at 60deg intervals instead of pairwise in 120deg intervals?


// CHANGELOG
// - 2025-07-14: Tightened nuts on last joint (where the arm is connected to the hexaplate) for better rigidity.
// - 2025-07-14: No PID (open loop) seems better.

