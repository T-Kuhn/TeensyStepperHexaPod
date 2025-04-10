// - [X] cleanup code
// - [X] Add a thing that let's us quickly switch between witch solution is used
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

