using CloudOStat.Meadow;

using Meadow.Foundation.Controllers.Pid;
using Meadow.Foundation.Sensors.Temperature;
using Meadow.Hardware;
using Meadow.Peripherals.Controllers.PID;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CloudOStat.LocalHardware
{
    public class HeatingElementController
    {
        float _targetTemperature;
        IPwmPort _heaterRelay;
        MAX31855 _tempSensor;
        IPidController _pidController;
        Thread _tempControlThread;
        int _updateInterval;

        public HeatingElementController(MAX31855 tempSensor, IPwmPort heater)
        {
            _tempSensor = tempSensor;
            _heaterRelay = heater;

            _pidController = new StandardPidController();
            _pidController.ProportionalComponent = .5f; // proportional
            _pidController.IntegralComponent = .05f; // integral time minutes
            _pidController.DerivativeComponent = 0f; // derivative time in minutes
            _pidController.OutputMin = 0.0f; // 0% power minimum
            _pidController.OutputMax = 1.0f; // 100% power max
            _pidController.OutputTuningInformation = true;

        }

        public void StartCook(int temp, int updateInterval)
        {
            _updateInterval = updateInterval;
            _targetTemperature = temp;
            // TEMP - to be replaced with PID stuff
            _heaterRelay.Frequency = 1.0f / 5.0f; // 5 seconds to start (later we can slow down)
            // on start, if we're under temp, turn on the heat to start.
            float duty = (_tempSensor.GetProbeTemperatureDataFahrenheit() < _targetTemperature) ? 1.0f : 0.0f;
            this._heaterRelay.DutyCycle = duty;
            this._heaterRelay.Start();

            // start our temp regulation thread. might want to change this to notify.
            //StartRegulatingTemperatureThread();
        }

        protected void StartRegulatingTemperatureThread()
        {
            _tempControlThread = new Thread(() =>
            {

                // reset our integral history
                _pidController.ResetIntegrator();

                // set our input and target on the PID calculator
                _pidController.ActualInput = (float)_tempSensor.GetProbeTemperatureDataFahrenheit();
                _pidController.TargetInput = _targetTemperature;

                // get the appropriate power level
                var powerLevel = _pidController.CalculateControlOutput();

                // set our PWM appropriately
                _heaterRelay.DutyCycle = powerLevel;
                Debug.WriteLine("power: " + powerLevel.ToString());

                // sleep for a while.
                Thread.Sleep(_updateInterval);
            });
            _tempControlThread.Start();
        }
    }
}
