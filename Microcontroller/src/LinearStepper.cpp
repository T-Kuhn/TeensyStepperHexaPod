/*
  LinearStepper library
  Author: T-Kuhn.
  Sapporo, January, 2020. Released into the public domain.
  */

#include "Arduino.h"
#include "LinearStepper.h"
#include "Constants.h"

  // - - - - - - - - - - - - - - -
  // - - - - CONSTRUCTOR - - - - -
  // - - - - - - - - - - - - - - -
LinearStepper::LinearStepper(uint8_t pinStep, uint8_t pinDir, uint8_t stepperID)
{
    currentPos = 0;
    id = stepperID;

    _pinStep = pinStep;
    _pinDir = pinDir;
    _isMovingCW = true;
    _goalPosition = 0;
    _currentStepsToTake = 0;
    _lastPulse = 0;

    pinMode(_pinStep, OUTPUT);
    pinMode(_pinDir, OUTPUT);
}

// - - - - - - - - - - - - - - -
// - - - - - UPDATE  - - - - - -
// - - - - - - - - - - - - - - -
void LinearStepper::update(float t)
{
    uint8_t pulse = pulseFromAmplitude(_currentStepsToTake, t);
    digitalWrite(_pinStep, pulse);
    _lastPulse = pulse;
}

// - - - - - - - - - - - - - - -
// - - - - SET GOAL POS  - - - -
// - - - - - - - - - - - - - - -
void LinearStepper::setGoalPos(int32_t goalPos)
{
    _goalPosition = goalPos;
    _currentStepsToTake = goalPos - currentPos;

    if (_currentStepsToTake > 0)
    {
        digitalWrite(_pinDir, LOW);
        _isMovingCW = true;
    }
    else
    {
        digitalWrite(_pinDir, HIGH);
        _isMovingCW = false;
    }
}

// - - - - - - - - - - - - - - -
// - SET STEPS TO TAKE TO ZERO -
// - - - - - - - - - - - - - - -
void LinearStepper::setStepsToTakeToZero()
{
    _currentStepsToTake = 0;
}

// - - - - - - - - - - - - - - -
// - -  PULSE FROM AMPLITUDE - -
// - - - - - - - - - - - - - - -
uint8_t LinearStepper::pulseFromAmplitude(float stepsToTake, float t)
{
    uint32_t doubledStepCount = (uint32_t)(round(t * abs(stepsToTake)));
    uint8_t stepLevel = doubledStepCount % 2;

    if (stepLevel > _lastPulse)
    {
        currentPos += _isMovingCW ? 1 : -1;
    }

    return stepLevel;
}