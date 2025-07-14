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
// Continue work on this ↓
// - [X] Add UI buttons to do the following: up/down at different speeds
// - [X] TEST WITH ARMS CONNECTED

// Things we now know after that test:
// - [X] test with NO-feedback-loop (no rotary encoder) stepper driver settings
// - [X] we probably need to put something soft under the arms on startup. Some arms are pushing agains the ground because of the way steppers work.
// - [X] Add apply-calibration button (applies all the values we found to be ideal for each motor)

// - [ ] we also want a button that makes the machine go up/down continuously. maybe 10 times with pauses in between (maybe multiple pause intervals?)
// - [ ] test with different stepper driver PID settings
// - [ ] we want to go up/down even faster
// - [ ] we want to go further up and down to a position above origin
// - [ ] Need to implement VirtualMachine for easier testing (we need to be able to send motor rotation instructions to the virtual machine)
// - [ ] test how feasible it would be to string together multiple short instructions to go up/down (we want to see how smooth a movement like that would be)
//     - depending on the results, we might want to...
//         - [ ] ...implement more complex movements like circling by stringing together multiple short instructions with the current sine-based movement system
//     OR
//         - [ ] ...implement a special start/continue/stop movement instruction where continue instructions keep on at the same speed and only start stop instructions use half of the current sine-based approach


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
