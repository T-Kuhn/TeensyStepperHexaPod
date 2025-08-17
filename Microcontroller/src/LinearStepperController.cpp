/*
  LinearStepperController library
  Author: T-Kuhn.
  Sapporo, January, 2020. Released into the public domain.
*/

#include "Constants.h"
#include "Arduino.h"
#include "LinearStepper.h"
#include "LinearStepperController.h"

// - - - - - - - - - - - - - - -
// - - - - CONSTRUCTOR - - - - -
// - - - - - - - - - - - - - - -
LinearStepperController::LinearStepperController(bool repeat)
{
    _endlessRepeat = repeat;
    _counter = 0;
    _numOfAttachedSteppers = 0;
    _currentMoveBatchIndex = 0;
    _isExecutingBatch = false;

    for (uint8_t i = 0; i < MAX_NUM_OF_STEPPERS; i++)
    {
        _linearSteppers[i] = { 0 };
    }
}

// - - - - - - - - - - - - - - -
// - - - - - ATTACH  - - - - - -
// - - - - - - - - - - - - - - -
void LinearStepperController::attach(LinearStepper* sStepper)
{
    if (sStepper->id < MAX_NUM_OF_STEPPERS)
    {
        _linearSteppers[sStepper->id] = sStepper;
        _numOfAttachedSteppers++;
    }
}

// - - - - - - - - - - - - - - - -
// - - RESET MOVEBATCH EXECUTION -
// - - - - - - - - - - - - - - - -
void LinearStepperController::resetMoveBatchExecution()
{
    _currentMoveBatchIndex = 0;
}

// - - - - - - - - - - - - - - -
// - - -  SET FREQ FROM  - - - -
// - - - - - - - - - - - - - - -
void LinearStepperController::setFrequencyFrom(float moveDuration)
{
    _frequency = FREQUENCY_MULTIPLIER / moveDuration;
}

// - - - - - - - - - - - - - - -
// - - - - - UPDATE  - - - - - -
// - - - - - - - - - - - - - - -
bool LinearStepperController::update()
{
    if (_currentMoveBatchIndex >= MAX_NUM_OF_MOVEBATCHES)
    {
        return false;
    }

    if (!_isExecutingBatch && _currentMoveBatchIndex < MAX_NUM_OF_MOVEBATCHES)
    {
        MoveBatch* mb = &moveBatches[_currentMoveBatchIndex];
        if (mb->needsExecution)
        {
            setFrequencyFrom(mb->moveDuration);
            for (uint8_t i = 0; i < _numOfAttachedSteppers; i++)
            {
                if (mb->moveCommands[i].isActive)
                {
                    _linearSteppers[i]->setGoalPos(mb->moveCommands[i].position);
                }
                else
                {
                    _linearSteppers[i]->setStepsToTakeToZero();
                }
            }
            mb->needsExecution = false;
            _isExecutingBatch = true;
        }
        else {
            // NOTE: If we tried to load a new move batch, but it didn't need execution,
            //       then it's save to assume that we reached the end of valid move batches
            //       and thus we are not anylonger doing meaningful work in here -> return false.
            return false;
        }
    }
    else
    {
        // GENERATE PULSES
        _counter++;
        // Theta goes from 0 ~ 1
        float t = _counter * _frequency;
        for (uint8_t i = 0; i < MAX_NUM_OF_STEPPERS; i++)
        {
            if (_linearSteppers[i] != 0)
            {
                _linearSteppers[i]->update(t);
            }
        }

        if (t > 1.0)
        {
            _isExecutingBatch = false;
            _counter = 0;
            _currentMoveBatchIndex++;
        }
    }

    return true;
}