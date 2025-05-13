import random
from typing import List, Tuple, Optional

class Card:
    def __init__(self, attack: int, health: int, price: int, cardID: str):
        self.attack = attack
        self.health = health
        self.price = price
        self.cardID = cardID  # Unique card identifier
        self.can_attack = False

    def __repr__(self):
         status = "ready" if self.can_attack else "waiting"
         return f"Card(A:{self.attack}, H:{self.health}, ${self.price}, {status})"

    def clone(self):
        """Create a copy of this card"""
        new_card = Card(self.attack, self.health, self.price, self.cardID)
        new_card.can_attack = self.can_attack
        return new_card

class Player:
    def __init__(self, name: str):
        self.name = name
        self.health = 30
        self.coins = 1
        self.board: List[Card] = []
        self.hand: List[Card] = []
        self.wallet: List[Card] = []

    def start_turn(self):
        self.coins += 1
        for card in self.board:
            card.can_attack = True
        print(f"{self.name} starts turn with {len(self.board)} cards and {self.coins} coins")


    def is_defeated(self):
        return self.health <= 0

class Game:
    def __init__(self, card_templates: List[Tuple[int, int, int, str]]):
        self.card_templates = card_templates
        self.shop: List[Card] = []

        # Initialize players
        self.q_ai_player = Player("Q-Learning AI")
        self.random_ai_player = Player("Random AI")

        self.current_player = self.q_ai_player
        self.opponent_player = self.random_ai_player
        self.turn_count = 1

        # Initialize shop and hands
        self.refresh_shop()
        self.deal_initial_hands()

    def refresh_shop(self):
        self.shop = []
        for _ in range(3):
            attack, health, price, cardID = random.choice(self.card_templates)
            self.shop.append(Card(attack, health, price, cardID))

    def deal_initial_hands(self):
        for player in [self.q_ai_player, self.random_ai_player]:
            player.hand = []  # Clear existing hand
            for _ in range(3):
                attack, health, price, cardID = random.choice(self.card_templates)
                player.hand.append(Card(attack, health, price, cardID))

    def buy_card(self, card_index: int) -> bool:
        if card_index < 0 or card_index >= len(self.current_player.hand):
            return False

        if len(self.current_player.wallet) >= 6:
            print("Wallet is full! Cannot buy more cards.")
            return False

        card = self.current_player.hand[card_index]
        if self.current_player.coins >= card.price:
            self.current_player.coins -= card.price
            self.current_player.wallet.append(card)
            self.current_player.hand.pop(card_index)
            return True
        return False

    def play_card(self, card_index: int) -> bool:
        if card_index < 0 or card_index >= len(self.current_player.wallet):
            return False

        card = self.current_player.wallet[card_index]
        self.current_player.board.append(card)
        self.current_player.wallet.pop(card_index)
        return True

    def attack(self, attacker_index: int, target_type: str, target_index: int = -1) -> bool:
        """Execute an attack action"""
        if attacker_index < 0 or attacker_index >= len(self.current_player.board):
            return False

        attacker = self.current_player.board[attacker_index]
        if not attacker.can_attack:
            return False

        attacker.can_attack = False

        if target_type == "player":
            self.opponent_player.health -= attacker.attack
            return True

        if target_type == "card":
            if target_index < 0 or target_index >= len(self.opponent_player.board):
                return False

            defender = self.opponent_player.board[target_index]
            defender.health -= attacker.attack
            attacker.health -= defender.health

            self.remove_dead_cards()
            return True

        return False

    def remove_dead_cards(self):
        """Remove cards with 0 or less health from both players' boards"""
        dead_cards_current = [card for card in self.current_player.board if card.health <= 0]
        dead_cards_opponent = [card for card in self.opponent_player.board if card.health <= 0]

        for card in dead_cards_current:
            print(f"Card removed from {self.current_player.name}'s board: {card}")
        for card in dead_cards_opponent:
            print(f"Card removed from {self.opponent_player.name}'s board: {card}")

        self.current_player.board = [card for card in self.current_player.board if card.health > 0]
        self.opponent_player.board = [card for card in self.opponent_player.board if card.health > 0]

    def switch_turn(self):
        """End current turn and start opponent's turn"""
        self.current_player, self.opponent_player = self.opponent_player, self.current_player

        # Start the new turn
        self.current_player.start_turn()
        self.turn_count += 1

        #Gain +1 mana every turn, like in original game
        self.current_player.coins += 1
        self.refresh_shop()

    def is_game_over(self) -> bool:
        """Check if the game is over"""
        return self.q_ai_player.health <= 0 or self.random_ai_player.health <= 0

    def get_winner(self) -> Optional[str]:
        """Return the winner of the game, None if game is not over"""
        if not self.is_game_over():
            return None
        if self.q_ai_player.health <= 0:
            return "random"
        return "q"

    def get_state_representation(self) -> Tuple:
        # Only include coins, both healths, and hand card stats (up to 3 cards)
        state = [
            self.current_player.coins,
            self.current_player.health,
            self.opponent_player.health
        ]
        def flatten_hand(cards, max_n=3):
            flat = []
            for c in cards[:max_n]:
                flat.extend([c.attack, c.health, c.price])
            while len(flat) < max_n * 3:
                flat.append(0)
            return flat
        state += flatten_hand(self.current_player.hand, 3)
        return tuple(state)

    def clone(self):
        """Create a copy of the current game state"""
        new_game = Game(self.card_templates)

        # Copy players
        new_game.q_ai_player.health = self.q_ai_player.health
        new_game.q_ai_player.coins = self.q_ai_player.coins
        new_game.q_ai_player.board = [card.clone() for card in self.q_ai_player.board]
        new_game.q_ai_player.hand = [card.clone() for card in self.q_ai_player.hand]
        new_game.q_ai_player.wallet = [card.clone() for card in self.q_ai_player.wallet]

        new_game.random_ai_player.health = self.random_ai_player.health
        new_game.random_ai_player.coins = self.random_ai_player.coins
        new_game.random_ai_player.board = [card.clone() for card in self.random_ai_player.board]
        new_game.random_ai_player.hand = [card.clone() for card in self.random_ai_player.hand]
        new_game.random_ai_player.wallet = [card.clone() for card in self.random_ai_player.wallet]

        # Set current player
        if self.current_player == self.q_ai_player:
            new_game.current_player = new_game.q_ai_player
            new_game.opponent_player = new_game.random_ai_player
        else:
            new_game.current_player = new_game.random_ai_player
            new_game.opponent_player = new_game.q_ai_player

        # Copy shop
        new_game.shop = [card.clone() for card in self.shop]
        new_game.turn_count = self.turn_count

        return new_game