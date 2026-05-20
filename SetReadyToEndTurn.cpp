// sts2, Version=0.1.0.0, Culture=neutral, PublicKeyToken=null
// MegaCrit.Sts2.Core.Combat.CombatManager
using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Runs;

public void SetReadyToEndTurn(Player player, bool canBackOut, Func<Task>? actionDuringEnemyTurn = null)
{
	using (_playerReadyLock.EnterScope())
	{
		if (_playersReadyToEndTurn.Contains(player))
		{
			return;
		}
		_playersReadyToEndTurn.Add(player);
	}
	this.PlayerEndedTurn?.Invoke(player, canBackOut);
	if (AllPlayersReadyToEndTurn())
	{
		Log.Debug("All players ready to end turn");
		GameAction currentlyRunningAction = RunManager.Instance.ActionExecutor.CurrentlyRunningAction;
		if (currentlyRunningAction != null && ActionQueueSet.IsGameActionPlayerDriven(currentlyRunningAction))
		{
			TaskHelper.RunSafely(WaitForActionThenEndTurn(currentlyRunningAction, actionDuringEnemyTurn));
		}
		else
		{
			TaskHelper.RunSafely(AfterAllPlayersReadyToEndTurn(actionDuringEnemyTurn));
		}
	}
}
