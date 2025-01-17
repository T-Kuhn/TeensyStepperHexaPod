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
// - [ ] add some sort of animation for end effector platform
//     - [X] moving up and down
//     - [X] moving left and right
//     - [X] moving forward and backward
//     - [ ] tilting around the X-Axis
//     - [ ] tilting around the Z-Axis
//     - [ ] circling (position)
//     - [ ] circling (tilt)
//     - [ ] moving up and down at left/right/front/back position
// - [ ] center gizmo would be nice, so we can tell how far away from center the hexaplate is
// - [ ] change colors of arm parts which are colliding so that we can get a better feeling of how the arm design has to be improved
// - [ ] iterate on arm design based on what we'll see in the simulated machine
// - [ ] Need a name for the thing. Some ideas:
//   - u-joint. The UJ-Table
//   - hexapod. The Hexa Bot.

// Thing to check:
// - Wouldn't it also work if the 6 arms were placed one at a time at 60deg intervals instead of pairwise in 120deg intervals?

