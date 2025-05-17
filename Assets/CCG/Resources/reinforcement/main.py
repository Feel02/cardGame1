import random
import tkinter as tk
from tkinter import ttk
import matplotlib.pyplot as plt
import json
import os
import threading
import numpy as np
from typing import List, Tuple

# Import your game and AI classes
from card_game import Game, Card, Player
from q_learning import QAgent

# Constants
DATA_FILE = "card_game_q_learning.json"
Q_TABLE_FILE = "rl_agent_qtable.json"

# Card templates with the cardID
CARD_TEMPLATES = [
    (1, 1, 1, "4bb0a4be44746cda3387cf7eece71cca"),  # Reticulocyte (1-rbc-1)
    (2, 2, 3, "2374de245aaf4d0832b26d01c3b3b095"),  # Erythrocytes (1-rbc-2)
    (2, 1, 1, "9e27331c549ec7a6f8bbf771b50fd965"),  # Myeloblast (2-wbc-1)
    (4, 2, 3, "c1acc2e36e4f74fa3477ff3239e5fb37"),  # Leukocytes (2-wbc-2)
    (2, 2, 2, "047aaaf95092a4ea49fae8be82a26576"),  # Innate Lymphocytes (3-nkc-1)
    (3, 2, 3, "551dd248415e929ce41c37199ae43433"),  # B Lymphocyte (4-bc-1)
    (3, 1, 3, "fb4adc920af6fcc699517b371560f4be"),  # Dendritiformis (5-dc-1)
    (1, 4, 3, "00f8c83501110154f58cc415de42626e"),  # T regulatoria (6-rtc-1)
    (3, 3, 5, "e2a474be5c962de665cc0a812403b052"),  # T auxiliaria (7-htc-1)
    (3, 1, 2, "09d1ca075c304459ba3b5628bed3f96e"),  # Naive T cytotoxica (8-ktc-1)
    (5, 2, 5, "6c93955eb084337f0bc15de64279bb7f"),  # T cytotoxica (8-ktc-2)
    (1, 1, 1, "5f1d9deb78c0846b4fcb2f65ab58004a"),  # Thrombocytus (9-plt-1)
    (2, 1, 2, "db6a60d892d32b22a49cf5902a1195eb"),  # Monocyte (10-mpc-1)
    (3, 4, 5, "867a4ebcedec6fb414a79d6866e11b0b"),  # Macrophagocytus (10-mpc-2)
    (3, 1, 3, "b4bad96f743c5fe3726135086883431e"),  # Campylobacter (11-cb-1)
    (2, 2, 2, "b9353846d496e0d714abb3d25d91fd61"),  # Staphylococcus aureus (12-stc-1)
    (3, 4, 4, "98346dc32f09b7e5b4d77d4aa52d2b8e"),  # Streptococcus pneumoniae (13-pc-1)
    (1, 1, 2, "bda249583a71b7aecd31f0c89a2364bf"),  # Eosinophilum (14-ep-1)
    (3, 2, 2, "6ad1a1a693fb4053b2c78f69e93a7ddf"),  # Vibrio (15-vb-1)
    (4, 1, 3, "cb737c6ab9422bbf081352ddddb52323"),  # Anisakis (16-as-1)
    (3, 2, 3, "ad08043a8af5763df5209483bb277199"),  # Basophilum (17-bp-1)
    (4, 2, 2, "83dc89f7f9ebff492be17dfefbb01f57"),  # Pseudomonas aeruginosa (18-pa-1)
    (5, 4, 5, "88162e568b68b3689c30ad84dba25faf"),  # Cellula cancerosa (19-cc-1)
    (2, 1, 1, "48377b439f8b92b9987ef55c9ae8a17b"),  # Virus influenzae (20-iv-1)
    (1, 3, 2, "a2bf30c3cc2145cd06d406eaae6bb799"),  # Allergia pollinis (21-pa-1)
]

# Game statistics
games_played = 0
q_ai_wins = 0
random_ai_wins = 0
draws = 0
game_progress = []
q_win_rates = []
random_win_rates = []
draw_rates = []

# Initialize Q-learning agent
q_agent = QAgent(alpha=0.1, gamma=0.9, epsilon=0.3)

# --- Data Saving/Loading ---
def save_data():
    """Save Q-table and game statistics to file"""
    data = {
        "games_played": games_played,
        "q_ai_wins": q_ai_wins,
        "random_ai_wins": random_ai_wins,
        "draws": draws,
        "game_progress": game_progress,
        "q_win_rates": q_win_rates,
        "random_win_rates": random_win_rates,
        "draw_rates": draw_rates
    }

    with open(DATA_FILE, "w") as file:
        json.dump(data, file, indent=4)

    print(f"Data saved to {DATA_FILE}")
    save_q_table_to_json() # Save Q-table as well

def load_data():
    """Load game statistics from file"""
    global games_played, q_ai_wins, random_ai_wins, draws
    global game_progress, q_win_rates, random_win_rates, draw_rates

    if not os.path.exists(DATA_FILE):
        print(f"No saved data file found at {DATA_FILE}")
        return

    try:
        with open(DATA_FILE, "r") as file:
            data = json.load(file)

        games_played = data["games_played"]
        q_ai_wins = data["q_ai_wins"]
        random_ai_wins = data["random_ai_wins"]
        draws = data["draws"]

        game_progress = data["game_progress"]
        q_win_rates = data["q_win_rates"]
        random_win_rates = data["random_win_rates"]
        draw_rates = data["draw_rates"]

        print(f"Data loaded from {DATA_FILE}")

    except Exception as e:
        print(f"Error loading data: {e}")

def save_q_table_to_json():
    """Saves the Q-table to a JSON file in Unity-compatible format"""
    try:
        serialized_q_table = {}
        for state, actions in q_agent.q_table.items():
            #State is a tuple, we need to convert it to string so it can be used as a key.
            state_str = f"{state[0]},{state[1]},{state[2]},{state[3]},{state[4]}"

            for action, value in actions.items():
                action_str = action_to_str(action)
                #Combine State and Action into a single key.
                key = f"{state_str}-{action_str}"
                serialized_q_table[key] = float(value)

        with open(Q_TABLE_FILE, 'w') as outfile:
            json.dump(serialized_q_table, outfile, indent=4)
        print(f"Q-table saved to {Q_TABLE_FILE}")

    except Exception as e:
        print(f"Error saving Q-table: {e}")

def load_q_table_from_json():
    """Loads the Q-table from a JSON file."""
    if not os.path.exists(Q_TABLE_FILE):
        print(f"No saved Q-table file found at {Q_TABLE_FILE}")
        return

    try:
        with open(Q_TABLE_FILE, 'r') as infile:
            loaded_q_table = json.load(infile)

        for key, value in loaded_q_table.items():
            state_str, action_str = key.split('-', 1)
            state = tuple(map(int, state_str.split(',')))
            action_tuple = parse_action_str(action_str)

            if state not in q_agent.q_table:
                q_agent.q_table[state] = {}

            q_agent.q_table[state][action_tuple] = value

        print(f"Q-table loaded from {Q_TABLE_FILE}")
        print(f"Q-table size: {len(q_agent.q_table)} states")

    except Exception as e:
        print(f"Error loading Q-table from JSON file: {e}")

def action_to_str(action_tuple):
    action_type = action_tuple[0]
    if action_type == 'buy':
        return f"BUY_{action_tuple[1]}"
    elif action_type == 'play':
        return f"PLAY_{action_tuple[1]}"
    elif action_type == 'attack_player':
        return f"ATTACK_{action_tuple[1]}_PLAYER"
    elif action_type == 'attack_card':
        return f"ATTACK_{action_tuple[1]}_CREATURE_{action_tuple[2]}"
    elif action_type == 'end_turn':
        return "END"
    else:
        raise ValueError(f"Unknown action type: {action_tuple[0]}")

def parse_action_str(action_str):
    parts = action_str.split('_')
    if parts[0] == "BUY":
        return ('buy', parts[1])
    elif parts[0] == "PLAY":
        return ('play', parts[1])
    elif parts[0] == "ATTACK":
        if parts[2] == "PLAYER":
            return ('attack_player', int(parts[1]))
        elif parts[2] == "CREATURE":
            return ('attack_card', int(parts[1]), int(parts[3]))
    elif parts[0] == "END":
        return ('end_turn',)
    raise ValueError(f"Unknown action string: {action_str}")

def random_ai_turn(game: Game):
    """Execute a turn for the random AI player"""
    print("\n=== RANDOM-AI TURN START ===")
    print(f"Random-AI has {game.current_player.coins} coins and {len(game.current_player.board)} cards on board")
    print(f"Q-AI has {len(game.opponent_player.board)} cards on board and {game.opponent_player.health} health")

    # --- Buying Cards ---
    # Try to buy a card (60% chance)
    if random.random() < 0.6 and len(game.current_player.wallet) < 6:
        if len(game.current_player.hand) > 0:
            # Find affordable cards
            affordable_cards = [i for i, card in enumerate(game.current_player.hand) if card.price <= game.current_player.coins]

            if affordable_cards:
                card_index = random.choice(affordable_cards) # Choose a random affordable card
                card_to_buy = game.current_player.hand[card_index]
                print(f"Random-AI attempting to buy card: {card_to_buy}")
                success = game.buy_card(card_index)
                if success:
                    print(f"Random-AI bought a card, now has {len(game.current_player.wallet)} cards in wallet and {len(game.current_player.hand)} cards in hand")
                else:
                    print("Random-AI could not buy card.")
            else:
                print("Random-AI cannot afford any cards.")

    # --- Playing Cards ---
    # Try to play a card from the wallet (60% chance)
    if random.random() < 0.6 and len(game.current_player.wallet) > 0:
        card_index = random.randint(0, len(game.current_player.wallet) - 1)
        print(f"Random-AI playing card from wallet {card_index}: {game.current_player.wallet[card_index]}")
        success = game.play_card(card_index)
        if success:
            print(f"Random-AI played a card, now has {len(game.current_player.board)} cards on board.")
        else:
            print("Random-AI couldn't play any cards from wallet.")

    # --- Attacking ---
    # Attack with random creature on the field
    attacker_indices = [i for i, card in enumerate(game.current_player.board) if card.can_attack]
    if attacker_indices:
        attacker_index = random.choice(attacker_indices)
        print(f"Random-AI attacking with card {attacker_index}: {game.current_player.board[attacker_index]}")

        #Decide Attack To the player or a unit
        if game.opponent_player.board and random.random() < 0.5: #50% chance to attack a card
            target_index = random.randint(0, len(game.opponent_player.board) - 1)
            print(f"Random-AI attacking opponent's card {target_index}: {game.opponent_player.board[target_index]}")
            success = game.attack(attacker_index, "card", target_index)
            if success:
                print("Attack on card successful")
            else:
                print("Attack on card failed.")

        else: #Attack Player
            print(f"Random-AI attacking player directly with card {attacker_index}: {game.current_player.board[attacker_index]}")
            success = game.attack(attacker_index, "player")
            if success:
                print(f"Attack successful! Q-AI health now: {game.opponent_player.health}")
            else:
                print("Attack on player failed")
    else:
        print("Random-AI has no creatures to attack with.")

    game.switch_turn()
    print("=== RANDOM-AI TURN END ===")

def q_ai_turn(game: Game):
    """Execute a turn for the Q-learning AI player (only learns which card to buy, always plays/attacks like random AI)"""
    global q_agent
    print("\n=== Q-AI TURN START ===")
    print(f"Q-AI has {game.current_player.coins} and {len(game.current_player.board)} cards on board")
    print(f"Random-AI has {len(game.opponent_player.board)} cards on board and {game.opponent_player.health} health")

    # --- BUY PHASE (Q-learning) ---
    bought_cards_this_turn = {}  # card_id: (state, action)
    bought_card_ids = set()
    while True:
        state = game.get_state_representation()
        possible_actions = q_agent.get_possible_actions(game)
        action = q_agent.choose_action(state, possible_actions)
        print(f"Q-AI choosing action: {action}")
        action_success = q_agent.execute_action(game, action)
        if action_success:
            print(f"Q-AI successfully performed action: {action}")
            if action[0] == 'buy':
                bought_cards_this_turn[action[1]] = (state, action)
                bought_card_ids.add(action[1])
        else:
            print(f"Q-AI failed to perform action: {action}")
        # Only keep buying if another buy is possible
        if action[0] != 'buy':
            break

    # --- PLAY PHASE (always play all cards from wallet) ---
    while len(game.current_player.wallet) > 0 and len(game.current_player.board) < 6:
        game.play_card(0)

    # --- ATTACK PHASE (every card must attack, more likely to attack player if enemy health is low) ---
    while any(card.can_attack for card in game.current_player.board):
        for i, card in enumerate(game.current_player.board):
            if card.can_attack:
                enemy_health = game.opponent_player.health
                if enemy_health <= 5:
                    attack_player_chance = 0.95
                elif enemy_health <= 10:
                    attack_player_chance = 0.8
                elif enemy_health <= 15:
                    attack_player_chance = 0.6
                elif enemy_health <= 20:
                    attack_player_chance = 0.4
                else:
                    attack_player_chance = 0.2
                if game.opponent_player.board and random.random() > attack_player_chance:
                    # Improved heuristic for attack target selection
                    best_score = float('-inf')
                    best_index = 0
                    for j, enemy_card in enumerate(game.opponent_player.board):
                        # Will my card kill the enemy?
                        will_enemy_die = card.attack >= enemy_card.health
                        # Will my card survive?
                        will_i_survive = enemy_card.attack < card.health
                        # Overkill amount (how much extra attack is wasted)
                        overkill = max(0, card.attack - enemy_card.health)
                        # Prefer using the weakest card that can kill the enemy
                        # Score: +100 for kill+survive, +20 for both die, -50 for lose my card, -overkill penalty
                        score = 0
                        if will_enemy_die and will_i_survive:
                            score += 100
                        elif will_enemy_die and not will_i_survive:
                            score += 20
                        elif not will_enemy_die and not will_i_survive:
                            score -= 50
                        # Bonus for killing high-value enemy
                        if will_enemy_die:
                            score += enemy_card.attack + enemy_card.health
                        # Penalize overkill
                        score -= overkill
                        # Prefer using lower attack cards for weak enemies
                        score -= card.attack * (1 if enemy_card.health == 1 else 0)
                        if score > best_score:
                            best_score = score
                            best_index = j
                    game.attack(i, "card", best_index)
                else:
                    game.attack(i, "player")
                break  # After attacking, break to re-check the board (since it may have changed)

    # --- Q-LEARNING REWARD FOR BOUGHT CARDS ---
    # For each card bought this turn, find it on the board and use its damage_dealt as reward
    for card_id, (state, action) in bought_cards_this_turn.items():
        # Find the card on the board (it may have died, so check both board and graveyard if you have one)
        total_damage = 0
        for card in game.current_player.board:
            if card.cardID == card_id:
                total_damage += card.damage_dealt
        # Optionally, if you have a graveyard, add damage from there too
        # If card is not found, it may have died without dealing damage
        # Use total_damage as reward for the buy action
        next_state = game.get_state_representation()
        possible_next_actions = q_agent.get_possible_actions(game)
        q_agent.update(state, action, total_damage, next_state, possible_next_actions)
        print(f"Q-AI buy action {action} got reward {total_damage}")

    game.switch_turn()
    print("=== Q-AI TURN END ===")

# --- Game Loop ---
def play_one_game():
    """Play a single game between Q-learning AI and random AI"""
    global games_played, q_ai_wins, random_ai_wins, draws, q_agent

    # Initialize a new game
    game = Game(CARD_TEMPLATES)
    print(f"\n====== STARTING GAME {games_played + 1} ======")

    # Add a maximum turn limit to prevent infinite games
    MAX_TURNS = 60

    # Track previous state for learning
    previous_state = game.get_state_representation()

    # Game loop
    while not game.is_game_over() and game.turn_count < MAX_TURNS:
        if game.current_player == game.q_ai_player:
            # Store state before Q-AI turn
            state_before = game.get_state_representation()

            # Execute Q-AI turn
            q_ai_turn(game)

            # Get state after turn
            state_after = game.get_state_representation()

            # Calculate reward and update Q-table
            reward = q_agent.calculate_reward(game, state_before, ('end_turn',), state_after)
            possible_next_actions = q_agent.get_possible_actions(game)
            q_agent.update(state_before, ('end_turn',), reward, state_after, possible_next_actions)

        else:
            random_ai_turn(game)

        print(f"Turn {game.turn_count} complete. Q-AI health: {game.q_ai_player.health}, Random-AI health: {game.random_ai_player.health}")

    # Determine winner
    games_played += 1

    if game.turn_count >= MAX_TURNS:
        print(f"Game {games_played} hit turn limit. Q-AI health: {game.q_ai_player.health}, Random-AI health: {game.random_ai_player.health}")
        if game.q_ai_player.health > game.random_ai_player.health:
            winner = "q"
        elif game.random_ai_player.health > game.q_ai_player.health:
            winner = "random"
        else:
            #This should not happen, but it is here in case both players have same amount of health
            winner = "q"
    else:
        winner = game.get_winner()

    # Update statistics
    if winner == "q":
        q_ai_wins += 1
        print(f"Q-AI wins game {games_played}!")
    elif winner == "random":
        random_ai_wins += 1
        print(f"Random-AI wins game {games_played}!")
    else:
        #This also should not happen, but kept here in case something goes wrong
        q_ai_wins += 1
        winner = "q"
        print(f"Q-AI wins game {games_played}!")

    # Update rates
    game_progress.append(games_played)
    q_win_rates.append(q_ai_wins / games_played)
    random_win_rates.append(random_ai_wins / games_played)
    draw_rates.append(draws / games_played)

    return winner

# --- Q-Table Saving and Loading ---
def save_q_table_to_json():
    """Saves the Q-table to a JSON file."""
    try:
        serialized_q_table = {}
        for state, actions in q_agent.q_table.items():
            state_str = str(state)
            serialized_q_table[state_str] = {}
            for action, value in actions.items():
                action_str = str(action)  # Serialize action
                serialized_q_table[f"{state_str}_{action_str}"] = value  # COMBINE state and action into a single key

        with open(Q_TABLE_FILE, 'w') as outfile:
            json.dump(serialized_q_table, outfile, indent=4)
        print(f"Q-table saved to {Q_TABLE_FILE}")
    except Exception as e:
        print(f"Error saving Q-table to JSON file: {e}")

def load_q_table_from_json():
    """Loads the Q-table from a JSON file."""
    if not os.path.exists(Q_TABLE_FILE):
        print(f"No saved Q-table file found at {Q_TABLE_FILE}")
        return

    try:
        with open(Q_TABLE_FILE, 'r') as infile:
            loaded_q_table = json.load(infile)

        # Convert string keys back to the Q-table structure
        for combined_key, value in loaded_q_table.items():
            state_str, action_str = combined_key.rsplit('_', 1)  # Split back into state and action
            state = eval(state_str)
            action = eval(action_str)

            if state not in q_agent.q_table:
                q_agent.q_table[state] = {}

            q_agent.q_table[state][action] = value

        print(f"Q-table loaded from {Q_TABLE_FILE}")
        print(f"Q-table size: {len(q_agent.q_table)} states")

    except Exception as e:
        print(f"Error loading Q-table from JSON file: {e}")

# Modify train_ai function to decrease epsilon over time
def train_ai(num_games=1000):
    """Train with adaptive exploitation strategy"""
    global games_played, q_ai_wins, random_ai_wins, draws
    
    initial_games = games_played
    initial_epsilon = q_agent.epsilon
    min_epsilon = 0.05  # Never go below 5% exploration
    
    # Recent win tracking for adaptive strategy
    recent_wins = []
    window_size = 50  # Track last 50 games
    
    for i in range(num_games):
        # Calculate win rate over recent window
        if len(recent_wins) >= window_size:
            recent_win_rate = sum(recent_wins) / window_size
            # If we're doing well, reduce epsilon faster to exploit more
            if recent_win_rate > 0.55:
                q_agent.epsilon = max(min_epsilon, q_agent.epsilon * 0.95)
            # If we're doing poorly, increase epsilon to explore more
            elif recent_win_rate < 0.4:
                q_agent.epsilon = min(0.4, q_agent.epsilon * 1.05)
            # Otherwise, normal decay
            else:
                q_agent.epsilon = max(min_epsilon, initial_epsilon * (1 - i/num_games))
        else:
            # Regular decay until we have enough history
            q_agent.epsilon = max(min_epsilon, initial_epsilon * (1 - i/num_games))
        
        # Play game and record win
        winner = play_one_game()
        recent_wins.append(1 if winner == "q" else 0)
        if len(recent_wins) > window_size:
            recent_wins.pop(0)
        
        # Periodic updates
        if (i+1) % 100 == 0:
            # Calculate win rate for recent games
            recent_rate = sum(recent_wins) / len(recent_wins) * 100
            print(f"Played {i+1}/{num_games} games. Recent win rate: {recent_rate:.1f}%")
            print(f"Current epsilon: {q_agent.epsilon:.3f}")
            save_data()

# --- UI ---
def create_ui():
    """Create the game UI with tkinter"""
    root = tk.Tk()
    root.title("Card Game AI Simulation")
    root.geometry("800x600")

    # Main frame
    frame = ttk.Frame(root, padding="10")
    frame.pack(fill=tk.BOTH, expand=True)

    # Statistics section
    stat_frame = ttk.LabelFrame(frame, text="Game Statistics", padding="10")
    stat_frame.pack(fill=tk.X, pady=10)

    # Labels for statistics
    stats_labels = {}

    stats_labels["games"] = ttk.Label(stat_frame, text=f"Games played: {games_played}")
    stats_labels["games"].grid(row=0, column=0, sticky=tk.W, pady=2)

    stats_labels["q_wins"] = ttk.Label(stat_frame, text=f"Q-Learning AI wins: {q_ai_wins} ({q_ai_wins/max(1, games_played)*100:.1f}%)")
    stats_labels["q_wins"].grid(row=1, column=0, sticky=tk.W, pady=2)

    stats_labels["random_wins"] = ttk.Label(stat_frame, text=f"Random AI wins: {random_ai_wins} ({random_ai_wins/max(1, games_played)*100:.1f}%)")
    stats_labels["random_wins"].grid(row=2, column=0, sticky=tk.W, pady=2)

    stats_labels["draws"] = ttk.Label(stat_frame, text=f"Draws: {draws} ({draws/max(1, games_played)*100:.1f}%)")
    stats_labels["draws"].grid(row=3, column=0, sticky=tk.W, pady=2)

    # Control section
    control_frame = ttk.LabelFrame(frame, text="Controls", padding="10")
    control_frame.pack(fill=tk.X, pady=10)

    # Training amount input
    ttk.Label(control_frame, text="Number of games:").grid(row=0, column=0, sticky=tk.W, pady=2)

    games_var = tk.StringVar(value="100")
    games_entry = ttk.Entry(control_frame, textvariable=games_var, width=10)
    games_entry.grid(row=0, column=1, sticky=tk.W, pady=2)

    # Learning rate and exploration rate
    ttk.Label(control_frame, text="Learning rate (α):").grid(row=1, column=0, sticky=tk.W, pady=2)
    alpha_var = tk.StringVar(value=str(q_agent.alpha))
    alpha_entry = ttk.Entry(control_frame, textvariable=alpha_var, width=10)
    alpha_entry.grid(row=1, column=1, sticky=tk.W, pady=2)

    ttk.Label(control_frame, text="Exploration rate (ε):").grid(row=2, column=0, sticky=tk.W, pady=2)
    epsilon_var = tk.StringVar(value=str(q_agent.epsilon))
    epsilon_entry = ttk.Entry(control_frame, textvariable=epsilon_var, width=10)
    epsilon_entry.grid(row=2, column=1, sticky=tk.W, pady=2)

    # Buttons
    def start_training():
        try:
            num_games = int(games_var.get())
            q_agent.alpha = float(alpha_var.get())
            q_agent.epsilon = float(epsilon_var.get())

            # Create a progress label
            progress_label = ttk.Label(stat_frame, text="Training in progress...")
            progress_label.grid(row=4, column=0, sticky=tk.W, pady=2)

            # Define function to run in thread with UI updates
            def run_training():
                for i in range(num_games):
                    play_one_game()

                    # Update UI every 10 games
                    if (i+1) % 10 == 0:
                        # Use after to safely update from thread
                        root.after(0, update_stats)
                        root.after(0, lambda: progress_label.config(
                            text=f"Training: {i+1}/{num_games} games completed ({(i+1)/num_games*100:.1f}%)"))

                    # Print progress
                    if (i+1) % 100 == 0:
                        print(f"Played {i+1} games. Q-AI wins: {q_ai_wins}, Random AI wins: {random_ai_wins}, Draws: {draws}")

                    # Save periodically
                    if (i+1) % 500 == 0:
                        save_data()

                # Save final data
                save_data()

                # Final UI update
                root.after(0, update_stats)
                root.after(0, lambda: progress_label.config(text=f"Training completed: {num_games} games"))

            # Run training in a separate thread
            training_thread = threading.Thread(target=run_training)
            training_thread.daemon = True  # Thread will exit when main program exits
            training_thread.start()

        except ValueError:
            print("Please enter valid numbers")

    def update_stats():
        stats_labels["games"].config(text=f"Games played: {games_played}")
        stats_labels["q_wins"].config(text=f"Q-Learning AI wins: {q_ai_wins} ({q_ai_wins/max(1, games_played)*100:.1f}%)")
        stats_labels["random_wins"].config(text=f"Random AI wins: {random_ai_wins} ({random_ai_wins/max(1, games_played)*100:.1f}%)")
        stats_labels["draws"].config(text=f"Draws: {draws} ({draws/max(1, games_played)*100:.1f}%)")

    def show_graph():
        plt.figure(figsize=(10, 6))
        plt.plot(game_progress, q_win_rates, 'g-', label='Q-Learning AI')
        plt.plot(game_progress, random_win_rates, 'r-', label='Random AI')
        plt.plot(game_progress, draw_rates, 'b-', label='Draws')
        plt.title('Win Rates Over Time')
        plt.xlabel('Games')
        plt.ylabel('Win Rate')
        plt.legend()
        plt.grid(True)
        plt.show()

    buttons_frame = ttk.Frame(control_frame)
    buttons_frame.grid(row=3, column=0, columnspan=2, pady=10)

    ttk.Button(buttons_frame, text="Train AI", command=start_training).pack(side=tk.LEFT, padx=5)
    ttk.Button(buttons_frame, text="Show Graph", command=show_graph).pack(side=tk.LEFT, padx=5)
    ttk.Button(buttons_frame, text="Save Data", command=save_data).pack(side=tk.LEFT, padx=5)

    # Return root to keep it in scope
    return root

# --- Main ---
if __name__ == "__main__":
    # Load existing data if available
    load_data()
    load_q_table_from_json()

    # Create and start UI
    root = create_ui()
    root.mainloop()