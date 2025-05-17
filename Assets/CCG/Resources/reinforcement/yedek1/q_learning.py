import random
from typing import Dict, List, Tuple, Optional
from card_game import Game

class Action:
    END_TURN = "end_turn"
    BUY_CARD = "buy"
    PLAY_CARD = "play"
    ATTACK_CARD = "attack_card"
    ATTACK_PLAYER = "attack_player"

    @staticmethod
    def end_turn() -> Tuple[str]:
        return (Action.END_TURN,)

    @staticmethod
    def buy_card(card_id: str) -> Tuple[str, str]:
        return (Action.BUY_CARD, card_id)

    @staticmethod
    def play_card(card_id: str) -> Tuple[str, str]:
        return (Action.PLAY_CARD, card_id)

    @staticmethod
    def attack_card(attacker_index: int, defender_index: int) -> Tuple[str, int, int]:
        return (Action.ATTACK_CARD, attacker_index, defender_index)

    @staticmethod
    def attack_player(attacker_index: int) -> Tuple[str, int]:
        return (Action.ATTACK_PLAYER, attacker_index)


class QAgent:
    """Q-Learning agent with advanced tactical state representation"""
    # State tuple structure:
    # [coins, your_health, opp_health, hand(3x3), wallet(3x3), your_board(3x3), opp_board(3x3), shop(3x3), counts: hand, wallet, your_board, opp_board, shop, opp_hand, opp_wallet, my_board_attack, my_board_health, opp_board_attack, opp_board_health, my_max_attack, opp_max_attack, lethal_on_board, danger, full_hand, full_wallet, full_board]
    
    def __init__(self, alpha=0.1, gamma=0.9, epsilon=0.3):
        """
        Initialize Q-learning agent
        
        Args:
            alpha: Learning rate (0-1)
            gamma: Discount factor (0-1)
            epsilon: Exploration rate (0-1)
        """
        self.alpha = alpha
        self.gamma = gamma
        self.epsilon = epsilon
        self.q_table = {}
        
        # Card value memory - track best cards to buy
        self.card_values = {}
        
        # Card pattern memory - track attack/health/cost patterns
        self.pattern_values = {}
        for attack in range(1, 10):
            for health in range(1, 10):
                for cost in range(1, 10):
                    if cost <= attack + health:  # Realistic cards
                        key = f"{attack}_{health}_{cost}"
                        self.pattern_values[key] = 0.5  # Initial positive bias
    
    def get_q_value(self, state: Tuple, action: Tuple) -> float:
        """Get Q-value for a state-action pair"""
        if state not in self.q_table:
            self.q_table[state] = {}
        if action not in self.q_table[state]:
            # Initialize with positive bias for attacks and buying
            if action[0] == 'attack_player':
                self.q_table[state][action] = 0.8  # Strong bias
            elif action[0] == 'buy':
                self.q_table[state][action] = 0.5  # Medium bias
            elif action[0] == 'play':
                self.q_table[state][action] = 0.3
            else:
                self.q_table[state][action] = 0.1
        return self.q_table[state][action]

    def choose_action(self, state: Tuple, possible_actions: List[Tuple]) -> Tuple:
        """Only use Q-learning for buy actions, else always buy if possible"""
        # If only pass_buy is available, return it
        if len(possible_actions) == 1 and possible_actions[0][0] == 'pass_buy':
            return possible_actions[0]
        # Epsilon-greedy for buy
        if random.random() < self.epsilon:
            return random.choice([a for a in possible_actions if a[0] == 'buy'])
        # Pick best buy
        best_action = None
        best_value = float('-inf')
        for action in possible_actions:
            if action[0] == 'buy':
                value = self.get_q_value(state, action)
                if value > best_value:
                    best_value = value
                    best_action = action
        if best_action:
            return best_action
        return random.choice(possible_actions)

    def _choose_best_buy_action(self, state: Tuple, buy_actions: List[Tuple]) -> Tuple:
        """Choose the best card to buy based on learning and heuristics"""
        best_action = None
        best_value = float('-inf')

        # With small chance, choose randomly to explore new cards
        if random.random() < 0.3:
            return random.choice(buy_actions)

        # Otherwise evaluate each card
        for action in buy_actions:
            card_id = action[1]

            # Get the card from game shop (this is a bit of a hack, but works for evaluation)
            for game in [g for g in [Game.current_game] if hasattr(Game, 'current_game')]:
                card = next((c for c in game.current_player.hand if c.cardID == card_id), None)

            # If we couldn't get the card info, use Q-value
            if not card:
                value = self.get_q_value(state, action)
            else:
                # Value based on attack/health and our learned preferences
                pattern_key = f"{card.attack}_{card.health}_{card.price}"
                pattern_value = self.pattern_values.get(pattern_key, 0)

                # Prefer high attack cards (aggressive strategy)
                attack_value = card.attack * 2
                health_value = card.health
                cost_value = card.price * -0.5

                # Combined value with bias toward attack
                value = attack_value + health_value + cost_value + pattern_value

            if value > best_value:
                best_value = value
                best_action = action

        if best_action:
            return best_action

        # Fallback to random if something went wrong
        return random.choice(buy_actions)

    def _choose_best_play_action(self, state: Tuple, play_actions: List[Tuple]) -> Tuple:
        """Choose the best card to play from wallet."""
        best_action = None
        best_value = float('-inf')

        # Evaluate each card
        for action in play_actions:
            card_id = action[1]
            value = self.get_q_value(state, action)  #Use only Q-Value for playing, for now
            if value > best_value:
                best_value = value
                best_action = action
        if best_action:
            return best_action

        # Fallback to random if something went wrong
        return random.choice(play_actions)

    def _choose_best_attack_player(self, state: Tuple, attack_actions: List[Tuple]) -> Tuple:
        """Choose the best card to attack player with"""
        # Simply use highest attack card (most damage)
        best_action = None
        best_damage = -1

        for action in attack_actions:
            attacker_id = action[1]
            for game in [g for g in [Game.current_game] if hasattr(Game, 'current_game')]:
                card = next((c for c in game.current_player.board if c.cardID == attacker_id), None)
                if card and card.attack > best_damage:
                    best_damage = card.attack
                    best_action = action

        if best_action:
            return best_action

        # Fallback to random
        return random.choice(attack_actions)

    def _choose_best_attack_card(self, state: Tuple, attack_actions: List[Tuple]) -> Tuple:
        """Choose the best card attack target, avoid bad trades if possible"""
        best_action = None
        best_target_value = -1

        for action in attack_actions:
            attacker_id = action[1]
            defender_id = action[2]

            for game in [g for g in [Game.current_game] if hasattr(Game, 'current_game')]:
                attacker = next((c for c in game.current_player.board if c.cardID == attacker_id), None)
                defender = next((c for c in game.opponent_player.board if c.cardID == defender_id), None)

                if attacker and defender:
                    # Avoid bad trades: if attacker will die and defender is not very valuable, skip
                    will_attacker_die = defender.attack >= attacker.health
                    will_defender_die = attacker.attack >= defender.health
                    target_value = defender.attack * 2
                    if will_defender_die and not will_attacker_die:
                        target_value += 10  # Good trade: kill and survive
                    elif will_defender_die and will_attacker_die:
                        target_value += 2   # Both die, small bonus
                    elif not will_defender_die and will_attacker_die:
                        target_value -= 10  # Bad trade: lose attacker, don't kill
                    # Bonus for killing high attack/valuable cards
                    if will_defender_die:
                        target_value += defender.attack + defender.health
                    # Avoid attacking if attacker will die and defender is weak
                    if will_attacker_die and defender.attack < attacker.attack:
                        target_value -= 8
                    if target_value > best_target_value:
                        best_target_value = target_value
                        best_action = action

        if best_action:
            return best_action
        return random.choice(attack_actions)

    def update(self, state: Tuple, action: Tuple, reward: float, next_state: Tuple,
               possible_next_actions: List[Tuple]):
        """Update Q-table using Q-learning update rule"""
        # Get current Q-value
        current_q = self.get_q_value(state, action)

        # Calculate max Q-value for next state
        max_next_q = 0.0
        if next_state is not None and possible_next_actions:
            max_next_q = max([self.get_q_value(next_state, next_action) for next_action in possible_next_actions])

        # Q-learning update rule with higher learning rate for critical actions
        adjusted_alpha = self.alpha
        if action[0] == 'attack_player':
            adjusted_alpha = min(1.0, self.alpha * 1.5)  # Learn faster for attacks

        # Update Q-value
        self.q_table[state][action] = current_q + adjusted_alpha * (reward + self.gamma * max_next_q - current_q)

        # Update card pattern learning for buy actions
        if action[0] == 'buy':
            card_id = action[1]

            # Try to get the card details
            for game in [g for g in [Game.current_game] if hasattr(Game, 'current_game')]:
                card = next((c for c in game.current_player.hand if c.cardID == card_id), None)

                if card:
                    pattern_key = f"{card.attack}_{card.health}_{card.price}"

                    # Update our pattern value
                    current_value = self.pattern_values.get(pattern_key, 0)
                    # Learn faster for higher rewards
                    learn_rate = min(0.3, 0.05 + abs(reward) * 0.01)
                    self.pattern_values[pattern_key] = current_value + learn_rate * (reward - current_value)

            # Also track specific card index value
            card_key = str(card_id)
            if card_key not in self.card_values:
                self.card_values[card_key] = []
            self.card_values[card_key].append(reward)

    def get_possible_actions(self, game: Game) -> List[Tuple]:
        """Only return buy actions for Q-learning, all else is always-play/attack-all"""
        Game.current_game = game
        actions = []
        # Only buy actions for Q-learning
        if len(game.current_player.wallet) < 6:
            for i, card in enumerate(game.current_player.hand):
                if card.price <= game.current_player.coins:
                    actions.append(('buy', card.cardID))
        if not actions:
            actions.append(('pass_buy',))
        return actions

    def execute_action(self, game: Game, action: Tuple) -> bool:
        """Execute the chosen action in the game"""
        Game.current_game = game  # Store reference
        action_type = action[0]

        if action_type == 'buy':
            card_id = action[1]
            # Find the card in hand by ID
            for i, card in enumerate(game.current_player.hand):
                if card.cardID == card_id:
                    return game.buy_card(i)  # Use the index once found
            print(f"Card with ID {card_id} not found in hand")
            return False

        elif action_type == 'play':
            card_id = action[1]
            # Find the card in wallet by ID
            for i, card in enumerate(game.current_player.wallet):
                if card.cardID == card_id:
                    return game.play_card(i)  # Use the index once found
            print(f"Card with ID {card_id} not found in wallet")
            return False

        elif action_type == 'attack_player':
            card_id = action[1]
            # Find the card on board by ID
            for i, card in enumerate(game.current_player.board):
                if card.cardID == card_id:
                    return game.attack(i, "player")
            print(f"Card with ID {card_id} not found on board for attack_player")
            return False

        elif action_type == 'attack_card':
            my_card_id = action[1]
            opp_card_id = action[2]
            my_index = next((i for i, c in enumerate(game.current_player.board) if c.cardID == my_card_id), None)
            opp_index = next((i for i, c in enumerate(game.opponent_player.board) if c.cardID == opp_card_id), None)
            if my_index is not None and opp_index is not None:
                return game.attack(my_index, "card", opp_index)
            print(f"Card with ID {my_card_id} or {opp_card_id} not found on board for attack_card")
            return False

        elif action_type == 'end_turn':
            game.switch_turn()
            return True

        return False

    def calculate_reward(self, game: Game, previous_state: Tuple, action: Tuple, current_state: Tuple) -> float:
        # previous_state: (coins, your_health, opp_health, hand1_a, hand1_h, hand1_p, hand2_a, hand2_h, hand2_p, hand3_a, hand3_h, hand3_p)
        prev_coins = previous_state[0]
        prev_q_health = previous_state[1]
        prev_opp_health = previous_state[2]
        curr_coins = current_state[0]
        curr_q_health = current_state[1]
        curr_opp_health = current_state[2]

        reward = 0.0

        # Reward for damaging opponent
        reward += (prev_opp_health - curr_opp_health) * 10.0
        # Penalty for losing health
        reward -= (prev_q_health - curr_q_health) * 2.0

        # Reward for buying a card (more for higher attack+health+cost)
        if action[0] == 'buy':
            # Find which card was bought (by comparing hand states)
            bought_card_stats = None
            for i in range(3):
                idx = 3 + i * 3
                if previous_state[idx:idx+3] != current_state[idx:idx+3]:
                    bought_card_stats = previous_state[idx:idx+3]
                    break
            if bought_card_stats:
                reward += bought_card_stats[0] * 2 + bought_card_stats[1] + bought_card_stats[2]
            else:
                reward += 5.0  # fallback
        if action[0] == 'pass_buy':
            reward -= 1.0  # small penalty for skipping buy

        # Small positive reward for any action
        reward += 0.1
        return reward